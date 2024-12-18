﻿namespace Girvs.Driven.Bus;

/// <summary>
/// 中介处理程序接口
/// 可以定义多个处理程序
/// 是异步的
/// </summary>
public interface IMediatorHandler : IManager
{
    /// <summary>
    /// 发送命令，将我们的命令模型发布到中介者模块
    /// </summary>
    /// <typeparam name="TCommand"> 泛型 </typeparam>
    /// <param name="command"> 命令模型，比如RegisterStudentCommand </param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<object> SendCommand<TCommand>(TCommand command, CancellationToken cancellationToken = default(CancellationToken))
        where TCommand : IBaseRequest;
    
    
    /// <summary>
    /// 发送命令，将我们的命令模型发布到中介者模块
    /// </summary>
    /// <param name="command">泛型</param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TCommand"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    /// <returns></returns>
    Task<TResponse> SendCommand<TCommand,TResponse>(TCommand command, CancellationToken cancellationToken = default(CancellationToken))
        where TCommand : IBaseRequest;


    /// <summary>
    /// 引发事件，通过总线，发布事件
    /// </summary>
    /// <typeparam name="T"> 泛型 继承 Event：INotification</typeparam>
    /// <param name="event"> 事件模型，比如StudentRegisteredEvent，</param>
    /// <param name="cancellationToken"></param>
    /// 请注意一个细节：这个命名方法和Command不一样，一个是RegisterStudentCommand注册学生命令之前,一个是StudentRegisteredEvent学生被注册事件之后
    /// <returns></returns>
    Task RaiseEvent<T>(T @event, CancellationToken cancellationToken = default(CancellationToken)) where T : Event;
}