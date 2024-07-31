namespace Girvs.Driven.Bus;

/// <summary>
/// 一个密封类，实现我们的中介内存总线
/// </summary>
public sealed class InMemoryBus(IMediator mediator) : IMediatorHandler
{
    // _serviceFactory = serviceFactory;
    /// <summary>
    /// 实现我们在IMediatorHandler中定义的接口
    /// 没有返回值
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="command"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<object> SendCommand<T>(
        T command,
        CancellationToken cancellationToken = default(CancellationToken)
    )
        where T : IBaseRequest => mediator.Send(command, cancellationToken);

    public async Task<TResponse> SendCommand<TCommand, TResponse>(
        TCommand command,
        CancellationToken cancellationToken = default(CancellationToken)
    )
        where TCommand : IBaseRequest => (TResponse)await SendCommand(command, cancellationToken);

    /// <summary>
    /// 引发事件的实现方法
    /// </summary>
    /// <typeparam name="T">泛型 继承 Event：INotification</typeparam>
    /// <param name="event">事件模型，比如StudentRegisteredEvent</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task RaiseEvent<T>(
        T @event,
        CancellationToken cancellationToken = default(CancellationToken)
    )
        where T : Event => mediator.Publish(@event, cancellationToken);
}
