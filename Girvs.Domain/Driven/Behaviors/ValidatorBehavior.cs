using System;
using System.Linq;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Girvs.Domain.Driven.Bus;
using Girvs.Domain.Driven.Commands;
using Girvs.Domain.Driven.Notifications;
using Girvs.Domain.Driven.Validations;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Girvs.Domain.Driven.Behaviors
{
    public class ValidatorBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : Command
    {
        private readonly IMediatorHandler _mediator;
        private readonly GirvsCommandValidator<TRequest> _validator;

        public ValidatorBehavior(IMediatorHandler mediator, IValidator<TRequest> validator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _validator = validator as GirvsCommandValidator<TRequest> ??
                         throw new ArgumentNullException(nameof(validator));
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken,
            RequestHandlerDelegate<TResponse> next)
        {
            var failures = (await _validator.ValidateAsync(request, cancellationToken)).Errors;
            if (failures.Any())
            {
                if (_validator.IsErrorMessageDelay) //较验错误信息通过通知的方式传递至前台,如果不启用延迟处理，而直接抛出异常
                {
                    foreach (var error in failures)
                    {
                        await _mediator.RaiseEvent(new DomainNotification(error.PropertyName, error.ErrorMessage));
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

            return await next();
        }
    }
}