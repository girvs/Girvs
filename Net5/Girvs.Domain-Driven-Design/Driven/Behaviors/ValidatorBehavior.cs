using System;
using System.Linq;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Girvs.Domain.Driven.Bus;
using Girvs.Domain.Driven.Notifications;
using Girvs.Domain.Driven.Validations;
using Girvs.Domain.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Girvs.Domain.Driven.Behaviors
{
    public class ValidatorBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly IMediatorHandler _mediator;
        private readonly ILogger<ValidatorBehavior<TRequest, TResponse>> _logger;

        public ValidatorBehavior(IMediatorHandler mediator, ILogger<ValidatorBehavior<TRequest, TResponse>> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken,
            RequestHandlerDelegate<TResponse> next)
        {
            _logger.LogInformation($"Command Validator Behavior : {typeof(TRequest).FullName}");
            //此处不能使用注入，针对不存在的较验会报错。
            if (EngineContext.Current.Resolve<IValidator<TRequest>>() is GirvsCommandValidator<TRequest> validator)
            {
                var failures = (await validator.ValidateAsync(request, cancellationToken)).Errors;
                if (failures.Any())
                {
                    //较验错误信息通过通知的方式传递至前台,如果不启用延迟处理，而直接抛出异常
                    if (validator.IsErrorMessageDelay)
                    {
                        foreach (var error in failures)
                        {
                            await _mediator.RaiseEvent(new DomainNotification(error.PropertyName, error.ErrorMessage), cancellationToken);
                        }

                        return default(TResponse);
                    }
                    else
                    {
                        var options = new JsonSerializerOptions
                        {
                            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(UnicodeRanges.All)
                        };

                        string error =
                            JsonSerializer.Serialize(failures.Select(x => new
                            {
                                PropertyName = x.PropertyName,
                                ErrorMessage = x.ErrorMessage
                            }), options);
                        throw new GirvsException(error, StatusCodes.Status422UnprocessableEntity);
                    }
                }
            }

            return await next();
        }
    }
}