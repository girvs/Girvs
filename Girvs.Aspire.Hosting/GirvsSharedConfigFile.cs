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
        return lazy.Value;
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
        await File.WriteAllTextAsync(path, merged, ct);
        return path;
    }
}
