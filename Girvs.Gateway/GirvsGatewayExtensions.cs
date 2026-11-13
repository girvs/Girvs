using Girvs.Gateway.FlowProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

namespace Girvs.Gateway;

/// <summary>注册基于统一服务目录的 YARP 反向代理与请求转换。</summary>
public static class GirvsGatewayExtensions
{
    public static IServiceCollection AddGirvsGateway(
        this IServiceCollection services,
        IConfiguration configuration = default
    )
    {
        services.AddSingleton<IProxyConfigProvider, GirvsGatewayProxyConfigProvider>();

        services
            .AddReverseProxy()
            .AddTransforms(context =>
            {
                context.AddOriginalHost(false);
                context.CopyRequestHeaders = true;
                // AddXForwarded 已包含 X-Forwarded-For，重复追加会得到 "ip, ip"，
                // 干扰下游对客户端 IP 的解析。
                context.AddXForwarded(ForwardedTransformActions.Append);
            });

        if (configuration is not null)
        {
            services.AddFlowProtection(configuration);
        }

        return services;
    }
}
