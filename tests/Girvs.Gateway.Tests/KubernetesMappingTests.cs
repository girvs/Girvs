using System.Collections.Generic;
using Girvs.Aspire.Gateway.Discovery;
using k8s.Models;

namespace Girvs.Aspire.Gateway.Tests;

public class KubernetesMappingTests
{
    private static V1Service Svc(string name, string ns, params int[] ports) => new()
    {
        Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns },
        Spec = new V1ServiceSpec
        {
            Ports = ports.Select(p => new V1ServicePort { Port = p }).ToList()
        }
    };

    [Fact]
    public void K8s服务映射为端点_集群内DNS地址()
    {
        var services = new[] { Svc("order-api", "default", 8080) };

        var endpoints = KubernetesGatewayServiceSource.MapServices(services);

        var ep = Assert.Single(endpoints);
        Assert.Equal("order-api", ep.ServiceName);
        var dest = Assert.Single(ep.Destinations);
        Assert.Equal("order-api-8080", dest.Key);
        Assert.Equal("http://order-api.default.svc.cluster.local:8080", dest.Value.Address);
    }

    [Fact]
    public void 多端口服务生成多目的地()
    {
        var services = new[] { Svc("api", "ns", 80, 443) };
        var ep = Assert.Single(KubernetesGatewayServiceSource.MapServices(services));
        Assert.Equal(2, ep.Destinations.Count);
    }
}
