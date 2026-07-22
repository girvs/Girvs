namespace Girvs.Refit.Tests;

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

        var endpoint = await resolver.ResolveAsync("partner", CancellationToken.None);

        Assert.Equal(new Uri("https://partner.example/api"), endpoint);
        Assert.True(resolver.CanResolve(RefitServiceAddressType.Static));
    }

    [Fact]
    public async Task Aspire解析器返回空以保留逻辑服务地址()
    {
        var resolver = new AspireRefitServiceEndpointResolver();

        var endpoint = await resolver.ResolveAsync("ordersservice", CancellationToken.None);

        Assert.Null(endpoint);
        Assert.True(resolver.CanResolve(RefitServiceAddressType.ServiceDiscovery));
    }
}
