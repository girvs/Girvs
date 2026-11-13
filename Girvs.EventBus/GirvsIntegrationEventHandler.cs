namespace Girvs.EventBus;

/// <summary>
/// Girvs 集成事件处理器。CAP 过滤器会在调用 <see cref="Handle"/> 前自动恢复身份和服务作用域。
/// </summary>
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
    /// 兼容旧处理器：在独立作用域内运行消费逻辑。
    /// </summary>
    [Obsolete("GirvsIntegrationEventHandler<T> 已由 CAP 过滤器自动建立身份和服务作用域，请直接在 Handle 中编写业务逻辑。")]
    protected Task HandleInScopeAsync(
        Func<CancellationToken, Task> body,
        CancellationToken cancellationToken
    )
    {
        if (body == null)
            throw new ArgumentNullException(nameof(body));

        return HandleInScopeCoreAsync((_, token) => body(token), cancellationToken);
    }

    /// <summary>
    /// 在一个贯穿整个异步执行期的独立作用域内运行消费逻辑，并把该作用域桥接到 EngineContext。
    /// </summary>
    [Obsolete("GirvsIntegrationEventHandler<T> 已由 CAP 过滤器自动建立身份和服务作用域，请直接在 Handle 中编写业务逻辑。")]
    protected Task HandleInScopeAsync(
        Func<IServiceProvider, CancellationToken, Task> body,
        CancellationToken cancellationToken
    )
    {
        if (body == null)
            throw new ArgumentNullException(nameof(body));

        return HandleInScopeCoreAsync(body, cancellationToken);
    }

    /// <summary>
    /// 在独立作用域内恢复 CAP 消息携带的身份后执行消费逻辑。
    /// </summary>
    [Obsolete("GirvsIntegrationEventHandler<T> 已由 CAP 过滤器自动建立身份和服务作用域，请直接在 Handle 中编写业务逻辑。")]
    protected Task HandleInScopeAsync(
        CapHeader header,
        Func<CancellationToken, Task> body,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        return HandleInScopeCoreAsync(header, (_, token) => body(token), cancellationToken);
    }

    /// <summary>
    /// 在独立作用域内恢复 CAP 消息携带的身份后执行消费逻辑。
    /// </summary>
    [Obsolete("GirvsIntegrationEventHandler<T> 已由 CAP 过滤器自动建立身份和服务作用域，请直接在 Handle 中编写业务逻辑。")]
    protected Task HandleInScopeAsync(
        CapHeader header,
        Func<IServiceProvider, CancellationToken, Task> body,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(header);
        ArgumentNullException.ThrowIfNull(body);

        return HandleInScopeCoreAsync(header, body, cancellationToken);
    }

    private async Task HandleInScopeCoreAsync(
        Func<IServiceProvider, CancellationToken, Task> body,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        using var _ = EngineContext.Current.ChangeCurrentThreadServiceProvider(
            scope.ServiceProvider);
        await body(scope.ServiceProvider, cancellationToken);
    }

    private async Task HandleInScopeCoreAsync(
        CapHeader header,
        Func<IServiceProvider, CancellationToken, Task> body,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        using var serviceProviderScope = EngineContext.Current.ChangeCurrentThreadServiceProvider(
            scope.ServiceProvider);
        var principalAccessor = scope.ServiceProvider
            .GetRequiredService<IGirvsPrincipalAccessor>();
        var principal = Identity.IntegrationEventPrincipalFactory.Create(header);
        using var principalScope = principalAccessor.Change(principal);
        await body(scope.ServiceProvider, cancellationToken);
    }

    public virtual void Dispose() { }
}
