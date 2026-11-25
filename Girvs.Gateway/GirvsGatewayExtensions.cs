using Girvs.Gateway.FlowProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

namespace Girvs.Gateway;

/// <summary>注册基于统一服务目录的 YARP 反向代理与请求转换。</summary>
/// <remarks>
/// <para>
/// 该扩展方法是网关接入的唯一入口，完成三件事：
/// 绑定并注册 <see cref="GirvsGatewayConfig"/>；注册基于 <see cref="IServiceDirectory"/> 的
/// 配置提供程序 <see cref="GirvsGatewayProxyConfigProvider"/>；启用 YARP 反向代理并配置全局请求转换。
/// </para>
/// <para>
/// 注意：本方法只注册服务，不会自动启动服务发现。宿主必须在 <c>Build()</c> 之后显式调用
/// 发现源的 <c>StartAsync</c>，否则网关拿不到任何路由。
/// </para>
/// </remarks>
public static class GirvsGatewayExtensions
{
    /// <summary>
    /// 将 Girvs 网关所需的服务、代理配置提供程序与请求转换注册到依赖注入容器。
    /// </summary>
    /// <param name="services">目标服务集合。</param>
    /// <param name="configuration">
    /// 可选的应用配置。用于绑定 <see cref="GirvsGatewayConfig"/>，并作为流程防护配置的来源；
    /// 传入 <see langword="null" /> 时使用 <see cref="GirvsGatewayConfig"/> 默认值且不注册流程防护。
    /// </param>
    /// <returns>便于链式调用的原 <paramref name="services" /> 实例。</returns>
    /// <remarks>
    /// 配置节读取顺序为 <c>ModuleConfigurations:GirvsGatewayConfig</c> →
    /// 顶级 <c>GirvsGatewayConfig</c> → 默认实例，保证宿主无论采用统一模块配置还是独立配置节都能生效。
    /// </remarks>
    public static IServiceCollection AddGirvsGateway(
        this IServiceCollection services,
        IConfiguration configuration = default
    )
    {
        // 三级回退：统一模块配置节 → 顶级同名配置节 → 代码默认值。
        var gatewayConfig = configuration?.GetSection("ModuleConfigurations:GirvsGatewayConfig").Get<GirvsGatewayConfig>()
            ?? configuration?.GetSection("GirvsGatewayConfig").Get<GirvsGatewayConfig>()
            ?? new GirvsGatewayConfig();
        services.AddSingleton(gatewayConfig);
        // 由服务目录快照推导路由/集群，并在服务变化时通过变更令牌驱动 YARP 热更新。
        services.AddSingleton<IProxyConfigProvider, GirvsGatewayProxyConfigProvider>();

        services
            .AddReverseProxy()
            .AddTransforms(context =>
            {
                // 使用下游 Host 而非客户端 Host，避免后端按 Host 做虚拟主机路由时匹配失败。
                context.AddOriginalHost(false);
                // 透传客户端原始请求头（含业务自定义头，流程防护依赖 X-Flow-Ticket 等头）。
                context.CopyRequestHeaders = true;
                // AddXForwarded 已包含 X-Forwarded-For，重复追加会得到 "ip, ip"，
                // 干扰下游对客户端 IP 的解析。
                context.AddXForwarded(ForwardedTransformActions.Append);
            });

        // 未提供配置时无法读取 FlowProtection 配置节，跳过注册；中间件仍可通过空配置构造。
        if (configuration is not null)
        {
            services.AddFlowProtection(configuration);
        }

        return services;
    }
}
