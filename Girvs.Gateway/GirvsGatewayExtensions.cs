using Consul;
using Girvs.Aspire.Gateway.Configuration;
using Girvs.Aspire.Gateway.Discovery;
using Girvs.Aspire.Gateway.FlowProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

namespace Girvs.Aspire.Gateway;

/// <summary>按配置的 <see cref="GatewayDiscoveryType"/> 选择服务发现源，并注册 YARP 反向代理与请求转换。</summary>
public static class GirvsGatewayExtensions
{
    public static IServiceCollection AddGirvsGateway(
        this IServiceCollection services,
        GatewayDiscoveryConfig config,
        IConfiguration configuration = default
    )
    {
        if (config.DiscoveryType == GatewayDiscoveryType.Kubernetes)
        {
            services.AddSingleton<IGatewayServiceDiscoverySource, KubernetesGatewayServiceSource>();
        }
        else if (config.DiscoveryType == GatewayDiscoveryType.Consul)
        {
            services.AddSingleton<IConsulClient>(
                _ =>
                    new ConsulClient(consulConfig =>
                    {
                        consulConfig.Address = new Uri(config.ConsulAddress);
                    })
            );
            services.AddSingleton<IGatewayServiceDiscoverySource, ConsulGatewayServiceSource>();
        }
        else
        {
            services.AddSingleton<IGatewayServiceDiscoverySource, AspireGatewayServiceSource>();
        }

        services.AddSingleton<IProxyConfigProvider, GirvsGatewayProxyConfigProvider>();

        services
            .AddReverseProxy()
            .AddTransforms(context =>
            {
                context.AddOriginalHost(false);
                context.CopyRequestHeaders = true;
                context.AddXForwarded(ForwardedTransformActions.Append);
                context.AddXForwardedFor("X-Forwarded-For", ForwardedTransformActions.Append);
            });

        if (configuration is not null)
        {
            services.AddFlowProtection(configuration);
        }

        return services;
    }
}
