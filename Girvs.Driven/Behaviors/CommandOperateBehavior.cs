namespace Girvs.Driven.Behaviors;

/// <summary>
/// 记录操作日志消息管道
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResponse"></typeparam>
public class CommandOperateBehavior<TRequest, TResponse>(
    ILogger<CommandOperateBehavior<TRequest, TResponse>> logger
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<CommandOperateBehavior<TRequest, TResponse>> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        // MediatR 14+（net10/net11）的委托带 CancellationToken 参数，显式传递以保留取消传播；
        // MediatR 11/12（net8/net9）为无参委托
#if NET10_0_OR_GREATER
        var result = await next(cancellationToken); //后至处理
#else
        var result = await next(); //后至处理
#endif

        if (EqualityComparer<TResponse>.Default.Equals(result, default))
            return result;

        _logger.LogDebug("Command Operate Behavior: {RequestType}", typeof(TRequest).FullName);

        if (request is not Command command || string.IsNullOrEmpty(command.CommandDesc))
            return result;

        var operateHandler = EngineContext.Current.Resolve<ICommandOperateHandler>();

        // await null Task 会抛 NRE，因此先判空再等待，处理器未注册时保持不抛
        if (operateHandler != null)
        {
            await operateHandler.Handle(command, cancellationToken);
        }

        //此处不能使用注入，针对不存在的校验会报错。
        return result;
    }
}
