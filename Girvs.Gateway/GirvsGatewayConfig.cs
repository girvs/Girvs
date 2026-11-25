using Girvs.Configuration;

namespace Girvs.Gateway;

/// <summary>
/// 网关模块配置（<see cref="IAppModuleConfig"/> 实现）。
/// </summary>
/// <remarks>
/// 由 <c>AddGirvsGateway</c> 从配置节 <c>ModuleConfigurations:GirvsGatewayConfig</c> 绑定读取，
/// 缺少该节点时回退到顶级 <c>GirvsGatewayConfig</c>，两者都缺失则使用本类型默认值。
/// 该配置只影响由服务目录动态生成的 YARP 集群的转发行为，不改变路由匹配规则。
/// </remarks>
public class GirvsGatewayConfig : IAppModuleConfig
{
    /// <summary>
    /// YARP 转发空闲超时，即两次转发 I/O 操作之间允许的最大间隔。
    /// 默认 5 分钟，大文件上传或下载场景可按需调大。
    /// </summary>
    /// <remarks>
    /// 该值会下发给每个动态生成集群的 <c>ForwarderRequestConfig.ActivityTimeout</c>，
    /// 用于限制“后端长时间不产生 I/O”的悬挂连接；它不同于请求总耗时上限，
    /// 只要后端持续有字节流动，连接就不会因本超时被中断。
    /// </remarks>
    public TimeSpan ActivityTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// 模块配置初始化钩子。本模块无需额外初始化动作，保持空实现以满足 <see cref="IAppModuleConfig"/> 契约。
    /// </summary>
    public void Init() { }
}
