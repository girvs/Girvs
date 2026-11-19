namespace Girvs.Driven.Bus;

/// <summary>
/// 一个密封类，实现我们的中介内存总线
/// </summary>
public sealed class InMemoryBus(IMediator mediator) : IMediatorHandler
{
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
        where T : IBaseRequest
    {
        return mediator.Send(command, cancellationToken); //请注意 入参 的类型
    }

    public async Task<TResponse> SendCommand<TCommand, TResponse>(
        TCommand command,
        CancellationToken cancellationToken = default(CancellationToken)
    )
        where TCommand : IBaseRequest
    {
        var obj = await SendCommand(command, cancellationToken);
        return (TResponse)obj;
    }

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
        where T : Event
    {
        // MediatR中介者模式中的第二种方法，发布/订阅模式
        return mediator.Publish(@event, cancellationToken);
    }
}
