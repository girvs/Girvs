using Girvs.Gateway.Configuration;
using Girvs.Gateway.Discovery;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.Gateway.Tests;

public class GirvsGatewayExtensionsTests
{
    [Fact]
    public void 默认配置_注册Aspire发现源与Provider()
    {
        var services = new ServiceCollection();
        services.AddGirvsGateway(new GatewayDiscoveryConfig());

        var descriptors = services.ToList();
        Assert.Contains(
            descriptors,
            d =>
                d.ServiceType == typeof(IGatewayServiceDiscoverySource)
                && d.ImplementationType == typeof(AspireGatewayServiceSource)
        );
        Assert.Contains(
            descriptors,
            d => d.ServiceType == typeof(Yarp.ReverseProxy.Configuration.IProxyConfigProvider)
        );
    }

    [Fact]
    public void 配置为Consul_注册Consul源与Provider()
    {
        var services = new ServiceCollection();
        services.AddGirvsGateway(new GatewayDiscoveryConfig { DiscoveryType = GatewayDiscoveryType.Consul });

        var descriptors = services.ToList();
        Assert.Contains(
            descriptors,
            d =>
                d.ServiceType == typeof(IGatewayServiceDiscoverySource)
                && d.ImplementationType == typeof(ConsulGatewayServiceSource)
        );
        Assert.Contains(
            descriptors,
            d => d.ServiceType == typeof(Yarp.ReverseProxy.Configuration.IProxyConfigProvider)
        );
    }

    [Fact]
    public void 配置为Kubernetes_注册K8s源与Provider()
    {
        var services = new ServiceCollection();
        services.AddGirvsGateway(new GatewayDiscoveryConfig { DiscoveryType = GatewayDiscoveryType.Kubernetes });

        var descriptors = services.ToList();
        Assert.Contains(descriptors, d => d.ServiceType == typeof(IGatewayServiceDiscoverySource)
            && d.ImplementationType == typeof(KubernetesGatewayServiceSource));
        Assert.Contains(descriptors, d => d.ServiceType == typeof(Yarp.ReverseProxy.Configuration.IProxyConfigProvider));
    }

    [Fact]
    public void GatewayDiscoveryConfig_支持Consul兼容配置()
    {
        var properties = typeof(GatewayDiscoveryConfig).GetProperties().Select(p => p.Name).ToArray();

        Assert.Contains("ConsulAddress", properties);
        Assert.DoesNotContain("ConsulConfig", properties);
        Assert.Contains("Consul", Enum.GetNames<GatewayDiscoveryType>());
    }
}
