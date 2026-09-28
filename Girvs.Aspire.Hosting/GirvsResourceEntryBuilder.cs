using Girvs.TypeFinder;


namespace Girvs.Aspire.Hosting;

/// <summary>
/// 把已登记的 Aspire 资源转换为 Girvs Resource 条目。
/// 匹配顺序:自动发现的提供程序(自定义在前,内置预设在后)
/// → AsGirvsResource 显式指定的 type/settings 兜底。
/// 内置预设见 SettingsProviders/ 目录(redis / mysql / sqlserver / rabbitmq / kafka),
/// Settings 键与各模块 BuildConnectionString 的消费约定对齐(见设计文档表格)。
/// </summary>
internal static class GirvsResourceEntryBuilder
{
    // 与框架模块机制同款的 TypeFinder 自动发现,内置预设与自定义提供程序走同一条路径:
    // 实现 IGirvsResourceSettingsProvider(公共无参构造)定义即生效,无需注册。
    // 用 AppDomainTypeFinder(WebAppTypeFinder 的 bin 目录扫描依赖 Web 宿主文件提供程序,
    // AppHost 进程没有),提供程序需定义在已加载的程序集——通常就是 AppHost 项目本身。
    // 排序:自定义(非本程序集)在前,可接管内置类型;再按类型全名排序保证确定性。
    private static readonly Lazy<IGirvsResourceSettingsProvider[]> Providers = new(() =>
        new AppDomainTypeFinder()
            .FindOfType<IGirvsResourceSettingsProvider>()
            .Where(type => type.GetConstructor(Type.EmptyTypes) is not null)
            .OrderBy(type => type.Assembly == typeof(GirvsResourceEntryBuilder).Assembly)
            .ThenBy(type => type.FullName, StringComparer.Ordinal)
            .Select(type => (IGirvsResourceSettingsProvider)Activator.CreateInstance(type))
            .ToArray()
    );

    public static async Task<KeyValuePair<string, GirvsInfrastructureResource>> BuildAsync(
        IResource resource,
        GirvsResourceAnnotation annotation,
        CancellationToken ct
    )
    {
        var built =
            await TryBuildFromProvidersAsync(Providers.Value, resource, ct)
            ?? (
                annotation.TypeOverride is not null
                    ? new GirvsInfrastructureResource { Type = annotation.TypeOverride }
                    : throw new InvalidOperationException(
                        $"AsGirvsResource 不支持资源类型 {resource.GetType().Name}:"
                            + "请实现 IGirvsResourceSettingsProvider(自动发现,无需注册),"
                            + "或显式指定 type 与 settings 参数"
                    )
            );

        // AsGirvsResource 显式参数覆盖提取结果
        if (annotation.TypeOverride is not null)
            built.Type = annotation.TypeOverride;
        foreach (var (key, value) in annotation.ExtraSettings)
            built.Settings[key] = value;

        return new KeyValuePair<string, GirvsInfrastructureResource>(resource.Name, built);
    }

    private static async Task<GirvsInfrastructureResource> TryBuildFromProvidersAsync(
        IReadOnlyList<IGirvsResourceSettingsProvider> providers,
        IResource resource,
        CancellationToken ct
    )
    {
        foreach (var provider in providers)
        {
            var built = await provider.TryBuildAsync(resource, ct);
            if (built is not null)
                return built;
        }

        return null;
    }
}
