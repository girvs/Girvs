using Girvs.Aspire.Gateway.Configuration;
using Girvs.Aspire.Gateway.Discovery;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.Aspire.Gateway.Tests;

public class GirvsGatewayExtensionsTests
{
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
    public void 配置为Consul_注册Consul源()
    {
        var services = new ServiceCollection();
        services.AddGirvsGateway(new GatewayDiscoveryConfig { DiscoveryType = GatewayDiscoveryType.Consul });

        Assert.Contains(services.ToList(), d => d.ServiceType == typeof(IGatewayServiceDiscoverySource)
            && d.ImplementationType == typeof(ConsulGatewayServiceSource));
    }
}
