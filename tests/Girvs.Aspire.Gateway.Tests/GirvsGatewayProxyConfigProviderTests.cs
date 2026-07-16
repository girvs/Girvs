using System.Collections.Generic;

namespace Girvs.Aspire.Gateway.Tests;

public class GirvsGatewayProxyConfigProviderTests
{
    private sealed class FakeSource : IGatewayServiceDiscoverySource
    {
        public List<GatewayServiceEndpoint> Services = new();
        public IReadOnlyList<GatewayServiceEndpoint> GetServices() => Services;
        public event Action? ServicesChanged;
        public Task StartAsync(CancellationToken ct) => Task.CompletedTask;
        public void Raise() => ServicesChanged?.Invoke();
    }

    private static GatewayServiceEndpoint Endpoint(string name) => new()
    {
        ServiceName = name,
        Destinations = new Dictionary<string, DestinationConfig>
        {
            [$"{name}-80"] = new DestinationConfig { Address = $"http://{name}:80" }
        }
    };

    [Fact]
    public void 每个服务生成约定路由与集群()
    {
        var source = new FakeSource { Services = { Endpoint("order_api") } };
        var provider = new GirvsGatewayProxyConfigProvider(source);

        var config = provider.GetConfig();
        var route = Assert.Single(config.Routes);
        Assert.Equal("order_api", route.RouteId);
        Assert.Equal("order_api", route.ClusterId);
        Assert.Equal("/order_api/{**catch-all}", route.Match.Path);
        Assert.Contains(route.Transforms!, t => t.TryGetValue("PathRemovePrefix", out var v) && v == "order_api");
        var cluster = Assert.Single(config.Clusters);
        Assert.Equal("order_api", cluster.ClusterId);
    }

    [Fact]
    public void 服务变化后_旧配置变更令牌被触发且新配置反映变化()
    {
        var source = new FakeSource { Services = { Endpoint("a") } };
        var provider = new GirvsGatewayProxyConfigProvider(source);
        var oldConfig = provider.GetConfig();
        Assert.False(oldConfig.ChangeToken.HasChanged);

        source.Services.Add(Endpoint("b"));
        source.Raise();

        Assert.True(oldConfig.ChangeToken.HasChanged); // 修掉了旧 bug：令牌真的接通
        var newConfig = provider.GetConfig();
        Assert.NotSame(oldConfig, newConfig);
        Assert.Equal(2, newConfig.Routes.Count);
    }

    [Fact]
    public void 重名服务去重()
    {
        var source = new FakeSource { Services = { Endpoint("a"), Endpoint("a") } };
        var provider = new GirvsGatewayProxyConfigProvider(source);
        Assert.Single(provider.GetConfig().Routes);
    }
}
