using System.Collections.Generic;
using Girvs.ServiceGovernance.Discovery;

namespace Girvs.Gateway.Tests;

public class GirvsGatewayProxyConfigProviderTests
{
    private sealed class FakeDirectory : IServiceDirectory
    {
        public List<DiscoveredService> Services = new();
        public IReadOnlyList<DiscoveredService> GetServices() => Services;
        public DiscoveredService? GetService(string serviceName) => Services.SingleOrDefault(x => x.ServiceName == serviceName);
        public event Action? ServicesChanged;
        public void Raise() => ServicesChanged?.Invoke();
    }

    private static DiscoveredService Endpoint(string name, bool gatewayEnabled = true) => new()
    {
        ServiceName = name,
        GatewayEnabled = gatewayEnabled,
        GatewayEndpointName = "http",
        Endpoints = [new DiscoveredServiceEndpoint { EndpointName = "http", Address = new Uri($"http://{name}:80") }]
    };

    [Fact]
    public void 每个服务生成约定路由与集群()
    {
        var directory = new FakeDirectory { Services = { Endpoint("order_api") } };
        var provider = new GirvsGatewayProxyConfigProvider(directory);

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
        var directory = new FakeDirectory { Services = { Endpoint("a") } };
        var provider = new GirvsGatewayProxyConfigProvider(directory);
        var oldConfig = provider.GetConfig();
        Assert.False(oldConfig.ChangeToken.HasChanged);

        directory.Services.Add(Endpoint("b"));
        directory.Raise();

        Assert.True(oldConfig.ChangeToken.HasChanged); // 修掉了旧 bug：令牌真的接通
        var newConfig = provider.GetConfig();
        Assert.NotSame(oldConfig, newConfig);
        Assert.Equal(2, newConfig.Routes.Count);
    }

    [Fact]
    public void 重名服务去重()
    {
        var directory = new FakeDirectory { Services = { Endpoint("a"), Endpoint("a") } };
        var provider = new GirvsGatewayProxyConfigProvider(directory);
        Assert.Single(provider.GetConfig().Routes);
    }

    [Fact]
    public void 并发触发服务变化不抛异常()
    {
        var directory = new FakeDirectory { Services = { Endpoint("a") } };
        var provider = new GirvsGatewayProxyConfigProvider(directory);

        // 并发多次触发 ServicesChanged（模拟 K8s watch 重连 relist 在多个线程池线程重叠触发），
        // 断言全程不抛异常（尤其不抛 ObjectDisposedException：旧实现里读-改-写非原子会导致重复 Cancel/Dispose）。
        var exception = Record.Exception(() =>
            Parallel.For(0, 1000, new ParallelOptions { MaxDegreeOfParallelism = 16 }, _ => directory.Raise()));

        Assert.Null(exception);
        // 触发过后仍可正常读取最新配置。
        var config = provider.GetConfig();
        Assert.False(config.ChangeToken.HasChanged);
        Assert.Single(config.Routes);
    }
}
