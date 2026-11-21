using System.Collections.Generic;
using Girvs.ServiceGovernance.Discovery;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
    public void 每个服务生成约定路由与根路由及集群()
    {
        var directory = new FakeDirectory { Services = { Endpoint("order_api") } };
        using var provider = new GirvsGatewayProxyConfigProvider(directory, new GirvsGatewayConfig());

        var config = provider.GetConfig();
        Assert.Equal(2, config.Routes.Count);

        var route = config.Routes.Single(r => r.RouteId == "order_api");
        Assert.Equal("order_api", route.ClusterId);
        Assert.Equal("/order_api/{**catch-all}", route.Match.Path);
        Assert.Contains(route.Transforms!, t => t.TryGetValue("PathRemovePrefix", out var v) && v == "order_api");

        // 服务根路径（无尾斜杠）单独一条路由，否则 "/order_api" 会 404。
        var rootRoute = config.Routes.Single(r => r.RouteId == "order_api-root");
        Assert.Equal("order_api", rootRoute.ClusterId);
        Assert.Equal("/order_api", rootRoute.Match.Path);
        Assert.Contains(rootRoute.Transforms!, t => t.TryGetValue("PathRemovePrefix", out var v) && v == "order_api");

        var cluster = Assert.Single(config.Clusters);
        Assert.Equal("order_api", cluster.ClusterId);
    }

    [Fact]
    public void 未指定选项时_所有集群的转发空闲超时默认五分钟()
    {
        var directory = new FakeDirectory { Services = { Endpoint("a"), Endpoint("b") } };
        using var provider = new GirvsGatewayProxyConfigProvider(directory);

        var clusters = provider.GetConfig().Clusters;
        Assert.Equal(2, clusters.Count);
        Assert.All(clusters, cluster =>
            Assert.Equal(TimeSpan.FromMinutes(5), cluster.HttpRequest?.ActivityTimeout));
    }

    [Theory]
    [InlineData(false, null, null, 5)]
    [InlineData(true, null, null, 5)]
    [InlineData(true, "00:10:00", null, 10)]
    [InlineData(true, null, "00:12:00", 12)]
    [InlineData(true, "00:10:00", "00:12:00", 10)]
    public void 注册网关时_配置的转发空闲超时应用于动态集群(
        bool provideConfiguration, string? moduleTimeout, string? topLevelTimeout, int expectedMinutes)
    {
        var settings = new Dictionary<string, string>();
        if (moduleTimeout is not null)
            settings["ModuleConfigurations:GirvsGatewayConfig:ActivityTimeout"] = moduleTimeout;
        if (topLevelTimeout is not null)
            settings["GirvsGatewayConfig:ActivityTimeout"] = topLevelTimeout;

        var directory = new FakeDirectory { Services = { Endpoint("a"), Endpoint("b") } };
        var services = new ServiceCollection();
        services.AddSingleton<IServiceDirectory>(directory);
        services.AddGirvsGateway(provideConfiguration
            ? new ConfigurationBuilder().AddInMemoryCollection(settings).Build()
            : null);
        using var serviceProvider = services.BuildServiceProvider();

        var provider = serviceProvider.GetRequiredService<IProxyConfigProvider>();
        var clusters = provider.GetConfig().Clusters;
        Assert.Equal(2, clusters.Count);
        Assert.All(clusters, cluster =>
            Assert.Equal(TimeSpan.FromMinutes(expectedMinutes), cluster.HttpRequest?.ActivityTimeout));
    }

    [Fact]
    public void 服务变化后_旧配置变更令牌被触发且新配置反映变化()
    {
        var directory = new FakeDirectory { Services = { Endpoint("a") } };
        var gatewayConfig = new GirvsGatewayConfig { ActivityTimeout = TimeSpan.FromMinutes(10) };
        using var provider = new GirvsGatewayProxyConfigProvider(directory, gatewayConfig);
        var oldConfig = provider.GetConfig();
        Assert.False(oldConfig.ChangeToken.HasChanged);

        directory.Services.Add(Endpoint("b"));
        directory.Raise();

        Assert.True(oldConfig.ChangeToken.HasChanged); // 修掉了旧 bug：令牌真的接通
        var newConfig = provider.GetConfig();
        Assert.NotSame(oldConfig, newConfig);
        Assert.Equal(4, newConfig.Routes.Count);
        Assert.Equal(2, newConfig.Clusters.Count);
        Assert.All(newConfig.Clusters, cluster =>
            Assert.Equal(TimeSpan.FromMinutes(10), cluster.HttpRequest?.ActivityTimeout));
    }

    [Fact]
    public void 重名服务去重()
    {
        var directory = new FakeDirectory { Services = { Endpoint("a"), Endpoint("a") } };
        using var provider = new GirvsGatewayProxyConfigProvider(directory, new GirvsGatewayConfig());
        Assert.Equal(2, provider.GetConfig().Routes.Count);
    }

    [Fact]
    public void 并发触发服务变化不抛异常()
    {
        var directory = new FakeDirectory { Services = { Endpoint("a") } };
        using var provider = new GirvsGatewayProxyConfigProvider(directory, new GirvsGatewayConfig());

        // 并发多次触发 ServicesChanged（模拟 K8s watch 重连 relist 在多个线程池线程重叠触发），
        // 断言全程不抛异常（尤其不抛 ObjectDisposedException：旧实现里读-改-写非原子会导致重复 Cancel/Dispose）。
        var exception = Record.Exception(() =>
            Parallel.For(0, 1000, new ParallelOptions { MaxDegreeOfParallelism = 16 }, _ => directory.Raise()));

        Assert.Null(exception);
        // 触发过后仍可正常读取最新配置。
        var config = provider.GetConfig();
        Assert.False(config.ChangeToken.HasChanged);
        Assert.Equal(2, config.Routes.Count);
    }
}
