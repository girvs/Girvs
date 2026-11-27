using System.Net;
using Girvs.Gateway.FlowProtection;
using Girvs.Cache.CacheImps;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Girvs.Gateway.Tests;

public class FlowProtectionRegistrationTests
{
    [Fact]
    public void 启用但未配置Redis资源_构建服务提供程序失败()
    {
        var services = new ServiceCollection();

        Assert.Throws<GirvsException>(
            () => services.AddFlowProtection(EnabledConfiguration())
        );
    }

    [Fact]
    public void 启用但缓存资源不是Redis_构建服务提供程序失败()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRedisConnectionWrapper, FakeRedisConnectionWrapper>();

        Assert.Throws<GirvsException>(
            () => services.AddFlowProtection(EnabledConfiguration(resourceType: "redis-synchronized-memory"))
        );
    }

    [Fact]
    public void 启用且缓存资源为Redis_注册流程保护服务()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRedisConnectionWrapper, FakeRedisConnectionWrapper>();

        services.AddFlowProtection(EnabledConfiguration(resourceType: "redis"));

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }
        );
        Assert.IsType<FlowDefinitionRegistry>(provider.GetRequiredService<FlowDefinitionRegistry>());
        Assert.IsType<RedisFlowStateStore>(provider.GetRequiredService<IFlowStateStore>());
        Assert.IsType<FlowTicketService>(provider.GetRequiredService<FlowTicketService>());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \t")]
    public async Task 建票组合键为空或空白_访问Redis前拒绝(string variantKey)
    {
        var store = new RedisFlowStateStore(new FakeRedisConnectionWrapper());
        var request = new FlowReserveRequest("ticket", "flow", "business", 0, "attempt", 300, 30, VariantKey: variantKey);

        await Assert.ThrowsAsync<ArgumentException>("request.VariantKey", () => store.CreateAndReserveAsync(request, default));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task 推进候选组合为Null或空集合_访问Redis前拒绝(bool useNull)
    {
        var store = new RedisFlowStateStore(new FakeRedisConnectionWrapper());
        var request = new FlowReserveRequest(
            "ticket", "flow", "business", 0, "attempt", 300, 30, CandidateVariantSteps: useNull ? null : []
        );

        await Assert.ThrowsAsync<ArgumentException>("request", () => store.ReserveAsync(request, default));
    }

    private static IConfiguration EnabledConfiguration(string resourceType = "")
    {
        var values = new Dictionary<string, string>
        {
            ["FlowProtection:Enabled"] = "true",
            ["FlowProtection:Flows:0:FlowId"] = "f1",
            ["FlowProtection:Flows:0:Interfaces:0:Name"] = "A",
            ["FlowProtection:Flows:0:Interfaces:0:Method"] = "POST",
            ["FlowProtection:Flows:0:Interfaces:0:Path"] = "/api/a",
            ["FlowProtection:Flows:0:Interfaces:1:Name"] = "B",
            ["FlowProtection:Flows:0:Interfaces:1:Method"] = "POST",
            ["FlowProtection:Flows:0:Interfaces:1:Path"] = "/api/b",
            ["FlowProtection:Flows:0:Variants:default:0"] = "A",
            ["FlowProtection:Flows:0:Variants:default:1"] = "B",
            ["ModuleConfigurations:CacheConfig:DistributedCacheConfig:Enabled"] = "true",
            ["ModuleConfigurations:CacheConfig:DistributedCacheConfig:ConnectionRef:0"] = "cache",
        };

        if (!string.IsNullOrWhiteSpace(resourceType))
        {
            values["Resources:cache:Type"] = resourceType;
            values["Resources:cache:Settings:Endpoints"] = "localhost:6379";
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private sealed class FakeRedisConnectionWrapper : IRedisConnectionWrapper
    {
        public string Instance => string.Empty;

        public Task<IDatabase> GetDatabaseAsync() => throw new NotSupportedException();

        public IDatabase GetDatabase() => throw new NotSupportedException();

        public Task<IServer> GetServerAsync(EndPoint endPoint) => throw new NotSupportedException();

        public Task<EndPoint[]> GetEndPointsAsync() => throw new NotSupportedException();

        public Task<ISubscriber> GetSubscriberAsync() => throw new NotSupportedException();

        public ISubscriber GetSubscriber() => throw new NotSupportedException();

        public Task FlushDatabaseAsync() => throw new NotSupportedException();
    }
}
