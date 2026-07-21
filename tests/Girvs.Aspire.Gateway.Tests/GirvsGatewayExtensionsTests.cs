using Girvs.Aspire.Gateway.Configuration;
using Girvs.Aspire.Gateway.Discovery;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.Aspire.Gateway.Tests;

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
    public void GatewayDiscoveryConfig_不再暴露Consul配置()
    {
        var properties = typeof(GatewayDiscoveryConfig).GetProperties().Select(p => p.Name).ToArray();

        Assert.DoesNotContain("ConsulAddress", properties);
        Assert.DoesNotContain("ConsulConfig", properties);
        Assert.DoesNotContain("Consul", Enum.GetNames<GatewayDiscoveryType>());
    }
}
