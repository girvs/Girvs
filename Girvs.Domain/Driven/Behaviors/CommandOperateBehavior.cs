using System;
using System.Threading;
using System.Threading.Tasks;
using Girvs.Domain.Driven.Bus;
using Girvs.Domain.Driven.Commands;
using Girvs.Domain.Infrastructure;
using Girvs.Domain.Managers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Girvs.Domain.Driven.Behaviors
{
    /// <summary>
    /// 记录操作日志消息管道
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    public class CommandOperateBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly IMediatorHandler _mediator;
        private readonly ILogger<CommandOperateBehavior<TRequest, TResponse>> _logger;

        public CommandOperateBehavior(
            IMediatorHandler mediator,
            ILogger<CommandOperateBehavior<TRequest, TResponse>> logger
            )
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            var result = await next(); //后至处理
            if (!result.Equals(default(TResponse)))
            {
                _logger.LogInformation($"Command Operate Behavior : {typeof(TRequest).FullName}");
                if (request is Command command)
                {
                    var operateHandler = EngineContext.Current.Resolve<ICommandOperateHandler>();
                    operateHandler?.Handle(command);
                }
            }

            //此处不能使用注入，针对不存在的较验会报错。
            return result;
        }
    }
}