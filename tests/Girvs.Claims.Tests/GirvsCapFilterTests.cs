namespace Girvs.Claims.Tests;

public class GirvsCapFilterTests
{
    [Fact]
    public async Task OnSubscribeExecutingAsync_Girvs处理器_自动恢复消息身份()
    {
        var (filter, accessor) = CreateFilter();
        var principal = CreatePrincipal("event-user");
        var context = CreateConsumerContext(
            typeof(TestIntegrationEventHandler),
            CreateHeaders(principal));

        await filter.OnSubscribeExecutingAsync(new ExecutingContext(context, []));

        Assert.Equal("event-user", accessor.Principal.GetUserId());
        Assert.Equal(ExecutionSource.EventBus, accessor.Principal.GetExecutionSource());
        Assert.True(accessor.Principal.Identity?.IsAuthenticated);
    }

    [Fact]
    public async Task OnSubscribeExecutedAsync_Girvs处理器_恢复进入前上下文()
    {
        var (filter, accessor) = CreateFilter();
        var original = CreatePrincipal("original");
        var context = CreateConsumerContext(
            typeof(TestIntegrationEventHandler),
            CreateHeaders(CreatePrincipal("event-user")));

        using (accessor.Change(original))
        {
            await filter.OnSubscribeExecutingAsync(new ExecutingContext(context, []));
            Assert.Equal("filter", EngineContext.Current.Resolve<ProviderMarker>().Name);

            await filter.OnSubscribeExecutedAsync(new ExecutedContext(context, null));

            Assert.Same(original, accessor.Principal);
            Assert.Equal("original", EngineContext.Current.Resolve<ProviderMarker>().Name);
        }
    }

    [Fact]
    public async Task OnSubscribeExceptionAsync_Girvs处理器_恢复上下文且不吞异常状态()
    {
        var (filter, accessor) = CreateFilter();
        var original = CreatePrincipal("original");
        var context = CreateConsumerContext(
            typeof(TestIntegrationEventHandler),
            CreateHeaders(CreatePrincipal("event-user")));
        var exceptionContext = new ExceptionContext(
            context,
            new InvalidOperationException("test"));

        using (accessor.Change(original))
        {
            await filter.OnSubscribeExecutingAsync(new ExecutingContext(context, []));
            await filter.OnSubscribeExceptionAsync(exceptionContext);

            Assert.Same(original, accessor.Principal);
            Assert.Equal("original", EngineContext.Current.Resolve<ProviderMarker>().Name);
            Assert.False(exceptionContext.ExceptionHandled);
        }
    }

    [Fact]
    public async Task OnSubscribeExecutingAsync_普通Cap处理器_不修改身份上下文()
    {
        var (filter, accessor) = CreateFilter();
        var original = CreatePrincipal("original");
        var context = CreateConsumerContext(
            typeof(PlainCapSubscriber),
            CreateHeaders(CreatePrincipal("event-user")));

        using (accessor.Change(original))
        {
            await filter.OnSubscribeExecutingAsync(new ExecutingContext(context, []));

            Assert.Same(original, accessor.Principal);
            Assert.Equal("original", EngineContext.Current.Resolve<ProviderMarker>().Name);
        }
    }

    [Fact]
    public async Task OnSubscribeExecutingAsync_没有身份头_使用空Principal()
    {
        var (filter, accessor) = CreateFilter();
        var original = CreatePrincipal("original");
        var context = CreateConsumerContext(
            typeof(TestIntegrationEventHandler),
            CreateCapHeaders());

        using (accessor.Change(original))
        {
            await filter.OnSubscribeExecutingAsync(new ExecutingContext(context, []));
            Assert.Empty(accessor.Principal.Claims);

            await filter.OnSubscribeExecutedAsync(new ExecutedContext(context, null));
            Assert.Same(original, accessor.Principal);
        }
    }

    [Fact]
    public async Task OnSubscribeExecutingAsync_身份访问器解析失败_恢复EngineContext()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new ProviderMarker("filter"));
        var provider = services.BuildServiceProvider();
        UseEngineWithOriginalProvider();
        var filter = new GirvsCapFilter(NullLogger<GirvsCapFilter>.Instance, provider);
        var context = CreateConsumerContext(
            typeof(TestIntegrationEventHandler),
            CreateHeaders(CreatePrincipal("event-user")));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            filter.OnSubscribeExecutingAsync(new ExecutingContext(context, [])));

        Assert.Equal("original", EngineContext.Current.Resolve<ProviderMarker>().Name);
    }

    [Fact]
    public async Task 两个Scoped过滤器并发消费_身份上下文互不共享()
    {
        var services = new ServiceCollection();
        services.AddHttpContextAccessor();
        services.AddSingleton<IGirvsPrincipalAccessor, GirvsPrincipalAccessor>();
        services.AddSingleton(new ProviderMarker("filter"));
        var provider = services.BuildServiceProvider();
        UseEngineWithOriginalProvider();
        var accessor = provider.GetRequiredService<IGirvsPrincipalAccessor>();

        async Task Consume(string userId)
        {
            var filter = new GirvsCapFilter(NullLogger<GirvsCapFilter>.Instance, provider);
            var context = CreateConsumerContext(
                typeof(TestIntegrationEventHandler),
                CreateHeaders(CreatePrincipal(userId)));

            await filter.OnSubscribeExecutingAsync(new ExecutingContext(context, []));
            await Task.Yield();
            Assert.Equal(userId, accessor.Principal.GetUserId());
            await filter.OnSubscribeExecutedAsync(new ExecutedContext(context, null));
            Assert.Empty(accessor.Principal.Claims);
        }

        await Task.WhenAll(Consume("user-a"), Consume("user-b"));
    }

    private static (GirvsCapFilter Filter, IGirvsPrincipalAccessor Accessor) CreateFilter()
    {
        var services = new ServiceCollection();
        services.AddHttpContextAccessor();
        services.AddSingleton<IGirvsPrincipalAccessor, GirvsPrincipalAccessor>();
        services.AddSingleton(new ProviderMarker("filter"));
        var provider = services.BuildServiceProvider();
        UseEngineWithOriginalProvider();
        return (
            new GirvsCapFilter(NullLogger<GirvsCapFilter>.Instance, provider),
            provider.GetRequiredService<IGirvsPrincipalAccessor>());
    }

    private static void UseEngineWithOriginalProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new ProviderMarker("original"));
        var engine = new TestEngine();
        engine.UseProvider(services.BuildServiceProvider());
        EngineContext.Replace(engine);
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

    private sealed class TestIntegrationEventHandler(IServiceProvider serviceProvider)
        : GirvsIntegrationEventHandler<IntegrationEvent>(serviceProvider)
    {
        public override Task Handle(
            IntegrationEvent @event,
            CapHeader header,
            CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class PlainCapSubscriber : ICapSubscribe;

    private sealed record ProviderMarker(string Name);

    private sealed class TestEngine : GirvsEngine
    {
        public void UseProvider(IServiceProvider provider)
        {
            ServiceProvider = provider;
        }
    }
}
