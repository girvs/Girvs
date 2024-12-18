﻿namespace Girvs.Driven.Bus;

/// <summary>
/// 一个密封类，实现我们的中介内存总线
/// </summary>
public sealed class InMemoryBus : IMediatorHandler
{
    //构造函数注入
    private readonly IMediator _mediator;

    //注入服务工厂
    // private readonly ServiceFactory _serviceFactory;
    //
    // private static readonly ConcurrentDictionary<Type, object> _requestHandlers =
    //     new ConcurrentDictionary<Type, object>();


    public InMemoryBus(IMediator mediator)
    {
        _mediator = mediator;
        // _serviceFactory = serviceFactory;
    }

    /// <summary>
    /// 实现我们在IMediatorHandler中定义的接口
    /// 没有返回值
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="command"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<object> SendCommand<T>(T command, CancellationToken cancellationToken = default(CancellationToken))
        where T : IBaseRequest
    {
        //这个是正确的
        return _mediator.Send(command, cancellationToken); //请注意 入参 的类型

        //注意！这个仅仅是用来测试和研究源码的，请开发的时候不要使用这个
        //return Send(command);//请注意 入参 的类型
    }

    public async Task<TResponse> SendCommand<TCommand, TResponse>(TCommand command,
        CancellationToken cancellationToken = default(CancellationToken)) where TCommand : IBaseRequest
    {
        var obj = await SendCommand(command, cancellationToken);
        return (TResponse)obj ;
    }

    /// <summary>
    /// 引发事件的实现方法
    /// </summary>
    /// <typeparam name="T">泛型 继承 Event：INotification</typeparam>
    /// <param name="event">事件模型，比如StudentRegisteredEvent</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task RaiseEvent<T>(T @event, CancellationToken cancellationToken = default(CancellationToken))
        where T : Event
    {
        // 除了领域通知以外的事件都保存下来
        //if (!@event.MessageType.Equals("DomainNotification"))
        //    _eventStoreService?.Save(@event);

        // MediatR中介者模式中的第二种方法，发布/订阅模式
        return _mediator.Publish(@event, cancellationToken);
    }
}