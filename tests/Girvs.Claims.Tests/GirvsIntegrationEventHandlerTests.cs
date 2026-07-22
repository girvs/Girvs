namespace Girvs.Claims.Tests;

public class GirvsIntegrationEventHandlerTests
{
    [Fact]
    public async Task HandleInScopeAsync_合法身份头_恢复身份并设置EventBus来源()
    {
        var (handler, accessor) = CreateHandler();
        var source = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(GirvsClaimTypes.UserId, "user-1"),
            new Claim(GirvsClaimTypes.IdentityType, IdentityType.ManagerUser.ToString())
        ], "Test"));
        var header = CreateHeader(source);

        await handler.ExecuteAsync(header, () =>
        {
            Assert.Equal("user-1", accessor.Principal.GetUserId());
            Assert.Equal(IdentityType.ManagerUser, accessor.Principal.GetIdentityType());
            Assert.Equal(ExecutionSource.EventBus, accessor.Principal.GetExecutionSource());
            Assert.True(accessor.Principal.Identity?.IsAuthenticated);
            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task HandleInScopeAsync_没有身份头_使用空Principal并在结束后恢复()
    {
        var (handler, accessor) = CreateHandler();
        var original = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(GirvsClaimTypes.UserId, "original")], "Test"));

        using (accessor.Change(original))
        {
            await handler.ExecuteAsync(new CapHeader(new Dictionary<string, string>()), () =>
            {
                Assert.Empty(accessor.Principal.Claims);
                return Task.CompletedTask;
            });

            Assert.Same(original, accessor.Principal);
        }
    }

    [Fact]
    public async Task HandleInScopeAsync_执行异常_恢复进入前身份()
    {
        var (handler, accessor) = CreateHandler();
        var original = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(GirvsClaimTypes.UserId, "original")], "Test"));

        using (accessor.Change(original))
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.ExecuteAsync(CreateHeader(CreatePrincipal("event-user")), () =>
                    throw new InvalidOperationException("test")));
            Assert.Same(original, accessor.Principal);
        }
    }

    private static (TestHandler Handler, IGirvsPrincipalAccessor Accessor) CreateHandler()
    {
        var services = new ServiceCollection();
        services.AddHttpContextAccessor();
        services.AddSingleton<IGirvsPrincipalAccessor, GirvsPrincipalAccessor>();
        var provider = services.BuildServiceProvider();
        var engine = new TestEngine();
        engine.UseProvider(provider);
        EngineContext.Replace(engine);
        return (new TestHandler(provider), provider.GetRequiredService<IGirvsPrincipalAccessor>());
    }

    private static CapHeader CreateHeader(ClaimsPrincipal principal)
    {
        return new CapHeader(new Dictionary<string, string>
        {
            [IntegrationIdentityContextSerializer.HeaderName] =
                IntegrationIdentityContextSerializer.Serialize(principal)
        });
    }

    private static ClaimsPrincipal CreatePrincipal(string userId) =>
        new(new ClaimsIdentity([new Claim(GirvsClaimTypes.UserId, userId)], "Test"));

    private sealed class TestHandler(IServiceProvider serviceProvider)
        : GirvsIntegrationEventHandler<IntegrationEvent>(serviceProvider)
    {
        public override Task Handle(
            IntegrationEvent @event,
            CapHeader header,
            CancellationToken cancellationToken) => Task.CompletedTask;

        public Task ExecuteAsync(CapHeader header, Func<Task> body) =>
            HandleInScopeAsync(header, _ => body(), CancellationToken.None);
    }

    private sealed class TestEngine : GirvsEngine
    {
        public void UseProvider(IServiceProvider provider)
        {
            ServiceProvider = provider;
        }
    }
}
