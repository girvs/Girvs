using GirvsResource = Girvs.Configuration.Resources.Resource;

namespace Girvs.Aspire.Hosting;

/// <summary>
/// Aspire 资源 → Girvs Resource 条目的提取扩展点:
/// 内置预设覆盖 redis / mysql / sqlserver / rabbitmq / kafka,
/// 其它资源类型(如 sqlite、elasticsearch)由业务方实现本接口并
/// 通过 <see cref="GirvsResourceSettingsProviders.Register"/> 注册。
/// </summary>
public interface IGirvsResourceSettingsProvider
{
    /// <summary>
    /// 尝试从 Aspire 资源提取 Girvs Resource(Type + Settings);
    /// 不能处理该资源时返回 null,交由后续提供程序处理。
    /// </summary>
    Task<GirvsResource> TryBuildAsync(IResource resource, CancellationToken ct);
}

/// <summary>
/// 提供程序注册表:自定义提供程序优先于内置预设匹配(同名覆盖内置行为)。
/// 与资源编排一样在 AppHost 启动早期注册,注册表不做并发防护。
/// </summary>
public static class GirvsResourceSettingsProviders
{
    private static readonly List<IGirvsResourceSettingsProvider> Custom = new();

    /// <summary>注册自定义提供程序,供业务方扩展自己的资源类型。</summary>
    public static void Register(IGirvsResourceSettingsProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        Custom.Add(provider);
    }

    /// <summary>移除已注册的提供程序(主要供测试隔离使用)。</summary>
    public static void Unregister(IGirvsResourceSettingsProvider provider) =>
        Custom.Remove(provider);

    internal static IReadOnlyList<IGirvsResourceSettingsProvider> CustomProviders => Custom;
}
