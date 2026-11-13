using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Girvs.Aspire.Hosting;

/// <summary>
/// 共享文件(girvs.shared.json)的幂等就地更新:首个服务的环境回调触发写入,后续服务复用同一结果。
/// 更新时机在服务启动前(环境回调求值时),此时被 WaitFor 的容器 endpoint 均已分配。
/// 文件不存在则创建;已存在则只更新 Resources 节点,其余手写内容原样保留(见 GirvsSharedConfigMerger)。
/// </summary>
internal static class GirvsSharedConfigFile
{
    public const string EnvName = "GIRVS_SHARED_CONFIG";
    public const string HandWrittenFileName = "girvs.shared.json";
    public const string PublishMountPath = "/girvs-config/girvs.shared.json";

    // 按 builder 实例缓存生成任务:AppHost 生命周期内只写一次,多服务并发回调安全。
    private static readonly ConditionalWeakTable<
        IDistributedApplicationBuilder,
        Lazy<Task<string>>
    > Cache = new();

    public static Task<string> EnsureWrittenAsync(
        IDistributedApplicationBuilder builder,
        CancellationToken ct
    )
    {
        var lazy = Cache.GetValue(
            builder,
            b => new Lazy<Task<string>>(() => WriteAsync(b, ct))
        );
        var task = lazy.Value;

        // Lazy<Task> 会永久缓存 canceled/faulted 结果。首个服务的回调被取消或写文件抛异常后，
        // 后续所有服务都会拿到同一个失败任务；移除缓存，让下一次回调重新尝试。
        if (task.IsCompleted && (task.IsFaulted || task.IsCanceled))
        {
            Cache.Remove(builder);
        }

        return task;
    }

    private static async Task<string> WriteAsync(
        IDistributedApplicationBuilder builder,
        CancellationToken ct
    )
    {
        var entries = new List<KeyValuePair<string, GirvsInfrastructureResource>>();
        foreach (var resource in builder.Resources)
        {
            var annotation = resource.Annotations.OfType<GirvsResourceAnnotation>().LastOrDefault();
            if (annotation is null)
                continue;
            entries.Add(await GirvsResourceEntryBuilder.BuildAsync(resource, annotation, ct));
        }

        var path = Path.Combine(builder.AppHostDirectory, HandWrittenFileName);
        var existingJson = File.Exists(path) ? await File.ReadAllTextAsync(path, ct) : null;

        var merged = GirvsSharedConfigMerger.Merge(existingJson, entries);

        // 先写临时文件再原子替换：写入中途取消/异常不会留下截断的 JSON，
        // 避免服务 reload 时解析失败。
        var tempPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(tempPath, merged, ct);
            File.Move(tempPath, path, overwrite: true);
        }
        catch
        {
            try
            {
                File.Delete(tempPath);
            }
            catch
            {
                // 清理失败不掩盖原始异常。
            }

            throw;
        }

        return path;
    }
}
