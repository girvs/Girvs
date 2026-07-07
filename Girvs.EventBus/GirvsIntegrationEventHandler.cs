namespace Girvs.EventBus;

public abstract class GirvsIntegrationEventHandler<TIntegrationEvent>(
    IServiceProvider serviceProvider
) : IIntegrationEventHandler<TIntegrationEvent>, IDisposable
    where TIntegrationEvent : IntegrationEvent
{
    private readonly IServiceProvider _serviceProvider =
        serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly IDisposable _ambientScopeToken =
        EngineContext.Current.ChangeCurrentThreadServiceProvider(serviceProvider);

    // 兜底：将本次消费的作用域登记为环境作用域。释放令牌时自动还原为切换前的值，
    // 而不是旧实现那样置 null —— 后者会污染同线程上其它并发消费的上下文。

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

        using var scope = _serviceProvider.CreateScope();
        using var _ = EngineContext.Current.ChangeCurrentThreadServiceProvider(
            scope.ServiceProvider
        );
        await body(cancellationToken);
    }

    public virtual void Dispose()
    {
        _ambientScopeToken?.Dispose();
    }
}
