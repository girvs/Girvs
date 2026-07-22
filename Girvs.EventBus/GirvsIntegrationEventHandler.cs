namespace Girvs.EventBus;

public abstract class GirvsIntegrationEventHandler<TIntegrationEvent>(
    IServiceProvider serviceProvider
) : IIntegrationEventHandler<TIntegrationEvent>, IDisposable
    where TIntegrationEvent : IntegrationEvent
{
    public abstract Task Handle(
        TIntegrationEvent @event,
        CapHeader header,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// 推荐用法：在一个贯穿整个异步执行期的独立作用域内运行消费逻辑，彻底避免与其它并发消费共享 DbContext。
    /// 由于 CAP 直接调用子类上标注 [CapSubscribe] 的方法，基类无法自动包裹，需子类在 Handle 中显式接入：
    /// <code>
    /// [CapSubscribe(nameof(XxxEvent))]
    /// public override Task Handle(XxxEvent e, CapHeader h, CancellationToken ct)
    ///     =&gt; HandleInScopeAsync(async token =&gt; { /* 原有消费逻辑，使用 token 作为取消令牌 */ }, ct);
    /// </code>
    /// </summary>
    protected async Task HandleInScopeAsync(
        Func<CancellationToken, Task> body,
        CancellationToken cancellationToken
    )
    {
        if (body == null)
            throw new ArgumentNullException(nameof(body));

        await HandleInScopeAsync((_, token) => body(token), cancellationToken);
    }

    /// <summary>
    /// 在一个贯穿整个异步执行期的独立作用域内运行消费逻辑，并把该作用域桥接到 EngineContext。
    /// </summary>
    protected async Task HandleInScopeAsync(
        Func<IServiceProvider, CancellationToken, Task> body,
        CancellationToken cancellationToken
    )
    {
        if (body == null)
            throw new ArgumentNullException(nameof(body));

        using var scope = serviceProvider.CreateScope();
        using var _ = EngineContext.Current.ChangeCurrentThreadServiceProvider(
            scope.ServiceProvider
        );
        await body(scope.ServiceProvider, cancellationToken);
    }

    /// <summary>
    /// 在独立作用域内恢复 CAP 消息携带的身份后执行消费逻辑。
    /// </summary>
    protected Task HandleInScopeAsync(
        CapHeader header,
        Func<CancellationToken, Task> body,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        return HandleInScopeAsync(header, (_, token) => body(token), cancellationToken);
    }

    /// <summary>
    /// 在独立作用域内恢复 CAP 消息携带的身份后执行消费逻辑。
    /// </summary>
    protected async Task HandleInScopeAsync(
        CapHeader header,
        Func<IServiceProvider, CancellationToken, Task> body,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(header);
        ArgumentNullException.ThrowIfNull(body);

        using var scope = serviceProvider.CreateScope();
        using var serviceProviderScope = EngineContext.Current.ChangeCurrentThreadServiceProvider(
            scope.ServiceProvider);
        var principalAccessor = scope.ServiceProvider
            .GetRequiredService<IGirvsPrincipalAccessor>();
        var principal = BuildPrincipal(header);
        using var principalScope = principalAccessor.Change(principal);
        await body(scope.ServiceProvider, cancellationToken);
    }

    private static ClaimsPrincipal BuildPrincipal(CapHeader header)
    {
        if (!header.TryGetValue(
                Identity.IntegrationIdentityContextSerializer.HeaderName,
                out var json) || string.IsNullOrEmpty(json))
        {
            return new ClaimsPrincipal();
        }

        var context = Identity.IntegrationIdentityContextSerializer.Deserialize(json);
        var claims = context.Claims
            .Where(x => x.Type != GirvsClaimTypes.ExecutionSource)
            .Select(x => new Claim(
                x.Type,
                x.Value,
                string.IsNullOrEmpty(x.ValueType) ? ClaimValueTypes.String : x.ValueType,
                string.IsNullOrEmpty(x.Issuer) ? ClaimsIdentity.DefaultIssuer : x.Issuer))
            .ToList();
        claims.Add(new Claim(
            GirvsClaimTypes.ExecutionSource,
            ExecutionSource.EventBus.ToString()));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Girvs.EventBus"));
    }

    public virtual void Dispose() { }
}
