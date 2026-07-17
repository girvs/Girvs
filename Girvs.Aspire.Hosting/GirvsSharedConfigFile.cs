using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Girvs.Aspire.Hosting;

/// <summary>
/// 运行时共享文件的幂等生成:首个服务的环境回调触发写入,后续服务复用同一结果。
/// 生成时机在服务启动前(环境回调求值时),此时被 WaitFor 的容器 endpoint 均已分配。
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
        var entries = new List<KeyValuePair<string, Girvs.Configuration.Resources.Resource>>();
        foreach (var resource in builder.Resources)
        {
            var annotation = resource.Annotations.OfType<GirvsResourceAnnotation>().LastOrDefault();
            if (annotation is null)
                continue;
            entries.Add(await GirvsResourceEntryBuilder.BuildAsync(resource, annotation, ct));
        }

        var handWrittenPath = Path.Combine(builder.AppHostDirectory, HandWrittenFileName);
        var handWrittenJson = File.Exists(handWrittenPath)
            ? await File.ReadAllTextAsync(handWrittenPath, ct)
            : null;

        var merged = GirvsSharedConfigMerger.Merge(handWrittenJson, entries);
        var outputPath = Path.Combine(
            builder.AppHostDirectory,
            "obj",
            "girvs.shared.runtime.json"
        );
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        await File.WriteAllTextAsync(outputPath, merged, ct);
        return outputPath;
    }
}
