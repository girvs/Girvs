namespace Girvs.Refit.Tests;

using Girvs.ServiceGovernance.Configuration;

public class ServiceEndpointResolverTests
{
    [Fact]
    public async Task 静态解析器返回配置端点()
    {
        var resolver = new StaticRefitServiceEndpointResolver(new RefitConfig
        {
            ServiceEndpoints = new Dictionary<string, string>
            {
                ["partner"] = "https://partner.example/api"
            }
        });

        var endpoint = await resolver.ResolveAsync("partner", null, CancellationToken.None);

        Assert.Equal(new Uri("https://partner.example/api"), endpoint);
        Assert.True(resolver.CanResolve(RefitServiceAddressType.Static));
    }

    [Fact]
    public async Task Aspire解析器返回空以保留逻辑服务地址()
    {
        var resolver = new AspireRefitServiceEndpointResolver();

        var endpoint = await resolver.ResolveAsync("ordersservice", null, CancellationToken.None);

        Assert.Null(endpoint);
        Assert.True(resolver.CanResolve(RefitServiceAddressType.ServiceDiscovery));
    }

    [Fact]
    public void Consul解析器优先使用ServiceGovernanceConfig地址()
    {
        var resolver = new ConsulRefitServiceEndpointResolver(
            new RefitConfig { ConsulAddress = "http://legacy-consul:8500" },
            new ServiceGovernanceConfig { ConsulAddress = "http://governance-consul:8500" }
        );

        Assert.Equal("http://governance-consul:8500", resolver.GetConsulAddress());
    }

    [Fact]
    public void ServiceGovernanceConfig地址为空时回退RefitConfig()
    {
        var resolver = new ConsulRefitServiceEndpointResolver(
            new RefitConfig { ConsulAddress = "http://legacy-consul:8500" },
            new ServiceGovernanceConfig { ConsulAddress = "" }
        );

        Assert.Equal("http://legacy-consul:8500", resolver.GetConsulAddress());
    }
}
