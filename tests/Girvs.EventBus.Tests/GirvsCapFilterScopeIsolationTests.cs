using System.Collections.Concurrent;

namespace Girvs.EventBus.Tests;

public class GirvsCapFilterScopeIsolationTests
{
    [Fact]
    public async Task 并发消费多条消息_每消息作用域与身份相互隔离()
    {
        // 消息根容器：按生产注册方式提供身份访问器，并注册每消息 Scoped 标记服务
        var messageServices = new ServiceCollection();
        messageServices.AddHttpContextAccessor();
        messageServices.AddSingleton<IGirvsPrincipalAccessor, GirvsPrincipalAccessor>();
        messageServices.AddScoped<ScopedMarker>();
        var messageProvider = messageServices.BuildServiceProvider();

        // 引擎容器：进入消息流之前 EngineContext 的兜底状态
        var engineServices = new ServiceCollection();
        engineServices.AddSingleton(new ProviderMarker("original"));
        var engine = new TestEngine();
        engine.UseProvider(engineServices.BuildServiceProvider());
        EngineContext.Replace(engine);

        var accessor = messageProvider.GetRequiredService<IGirvsPrincipalAccessor>();
        var markers = new ConcurrentBag<ScopedMarker>();

        // 模拟 CAP 消费结构：每条消息 CreateAsyncScope → 从该 scope 构造过滤器
        // → Executing（带身份 header）→ 流内解析 → Executed → scope dispose
        async Task Consume(string userId)
        {
            await using var scope = messageProvider.CreateAsyncScope();
            var filter = new GirvsCapFilter(
                NullLogger<GirvsCapFilter>.Instance,
                scope.ServiceProvider);
            var context = CreateConsumerContext(
                typeof(TestIntegrationEventHandler),
                CreateHeaders(CreatePrincipal(userId)));

            await filter.OnSubscribeExecutingAsync(new ExecutingContext(context, []));
            await Task.Yield(); // 让并发流真实交错

            // 断言 1：流内经 EngineContext 解析 Scoped 标记服务，稳定命中本消息 scope 的实例
            var marker = EngineContext.Current.Resolve<ScopedMarker>();
            Assert.NotNull(marker);
            Assert.Same(marker, EngineContext.Current.Resolve<ScopedMarker>());
            markers.Add(marker);

            // 断言 2：principal 为本消息 header 恢复的身份，跨流不串
            Assert.Equal(userId, accessor.Principal.GetUserId());
            // 桥接期间引擎解析落在本消息 scope（引擎容器未注册 ProviderMarker）
            Assert.Null(EngineContext.Current.Resolve<ProviderMarker>());

            await filter.OnSubscribeExecutedAsync(new ExecutedContext(context, null));

            // 断言 3：Executed 后 EngineContext 回退到进入前状态（引擎容器的 original 标记）
            Assert.Equal("original", EngineContext.Current.Resolve<ProviderMarker>().Name);
            Assert.Empty(accessor.Principal.Claims);

            // 断言 4：scope 释放后流内不再持有已释放的 provider，解析回退到引擎容器
            await scope.DisposeAsync();
            Assert.Null(EngineContext.Current.Resolve<ScopedMarker>());
        }

        var userNames = Enumerable.Range(0, 8).Select(i => $"user-{i}").ToArray();
        await Task.WhenAll(userNames.Select(name => Task.Run(() => Consume(name))));

        // 每消息 scope 各自解析到不同的 Scoped 标记服务实例
        Assert.Equal(userNames.Length, markers.Distinct().Count());
    }

    private static ConsumerContext CreateConsumerContext(
        Type handlerType,
        IDictionary<string, string> headers)
    {
        var descriptor = new ConsumerExecutorDescriptor
        {
            ImplTypeInfo = handlerType.GetTypeInfo()
        };
        var mediumMessage = new MediumMessage
        {
            Origin = new Message(headers, new object())
        };
        return new ConsumerContext(descriptor, mediumMessage);
    }

    private static Dictionary<string, string> CreateHeaders(ClaimsPrincipal principal) =>
        new(CreateCapHeaders())
        {
            [IntegrationIdentityContextSerializer.HeaderName] =
                IntegrationIdentityContextSerializer.Serialize(principal)
        };

    private static Dictionary<string, string> CreateCapHeaders() =>
        new()
        {
            ["cap-msg-name"] = nameof(IntegrationEvent),
            ["cap-corr-id"] = Guid.NewGuid().ToString()
        };

    private static ClaimsPrincipal CreatePrincipal(string userId) =>
        new(new ClaimsIdentity([new Claim(GirvsClaimTypes.UserId, userId)], "Test"));

    // 每消息 Scoped 标记服务：按引用比较即可验证各消息实例互不相同
    private sealed class ScopedMarker;

    private sealed class TestIntegrationEventHandler
        : GirvsIntegrationEventHandler<IntegrationEvent>
    {
        public override Task Handle(
            IntegrationEvent @event,
            CapHeader header,
            CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed record ProviderMarker(string Name);

    private sealed class TestEngine : GirvsEngine
    {
        public void UseProvider(IServiceProvider provider)
        {
            ServiceProvider = provider;
        }
    }
}
