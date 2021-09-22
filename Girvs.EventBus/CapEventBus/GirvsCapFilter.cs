using System;
using DotNetCore.CAP.Filter;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Girvs.EventBus.CapEventBus
{
    public class GirvsCapFilter : ISubscribeFilter
    {
        private readonly ILogger _logger;

        public GirvsCapFilter([NotNull] ILogger<GirvsCapFilter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void OnSubscribeExecuting(ExecutingContext context)
        {
            _logger.LogInformation(
                $"^^^^^^^^^^^^^^^^^^^^^^^^^{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}###订阅收到消息：{context.DeliverMessage.Headers["cap-msg-name"]}^^^^^^^^^^^^^^^^^^^^^^^^^^");
            _logger.LogInformation(
                $"^^^^^^^^^^^^^^^^^^^^^^^^^^开始处理订阅的消息:{context.DeliverMessage.Headers["cap-corr-id"]}^^^^^^^^^^^^^^^^^^^^^^^^^^");
        }

        public void OnSubscribeExecuted(ExecutedContext context)
        {
            _logger.LogInformation(
                $"^^^^^^^^^^^^^^^^^^^^^^^^^^{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}###订阅收到消息处理结束  Name:{context.DeliverMessage.Headers["cap-msg-name"]} Id:{context.DeliverMessage.Headers["cap-corr-id"]}");
        }

        public void OnSubscribeException(ExceptionContext context)
        {
            _logger.LogInformation(
                $"^^^^^^^^^^^^^^^^^^^^^^^^^^{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}###订阅收到消息处理出现异常Name:{context.DeliverMessage.Headers["cap-msg-name"]} Id:{context.DeliverMessage.Headers["cap-corr-id"]}^^^^^^^^^^^^^^^^^^^^^^^^^^");
            _logger.LogError(context.Exception, context.Exception.Message);
        }
    }
}
