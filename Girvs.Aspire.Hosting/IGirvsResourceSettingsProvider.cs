using GirvsResource = Girvs.Configuration.Resources.Resource;

namespace Girvs.Aspire.Hosting;

/// <summary>
/// Aspire 资源 → Girvs Resource 条目的提取扩展点:
/// 内置预设覆盖 redis / mysql / sqlserver / rabbitmq / kafka,
/// 其它资源类型(如 sqlite、mongodb、elasticsearch)由业务方实现本接口。
/// 与框架模块机制一样通过 TypeFinder 反射自动发现——定义实现类即生效,无需注册;
/// 要求有公共无参构造函数,自定义提供程序优先于内置预设匹配(可接管内置类型)。
/// </summary>
public interface IGirvsResourceSettingsProvider
{
    /// <summary>
    /// 尝试从 Aspire 资源提取 Girvs Resource(Type + Settings);
    /// 不能处理该资源时返回 null,交由后续提供程序处理。
    /// </summary>
    Task<GirvsResource> TryBuildAsync(IResource resource, CancellationToken ct);
}
