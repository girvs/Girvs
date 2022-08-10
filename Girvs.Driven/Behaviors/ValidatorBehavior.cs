namespace Girvs.Driven.Behaviors
{
    public class ValidatorBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
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
                            await _mediator.RaiseEvent(new DomainNotification(error.PropertyName, error.ErrorMessage),
                                cancellationToken);
                        }

                        return default(TResponse);
                    }
                    else
                    {
                        var errorDictionary = new Dictionary<string, IList<string>>();

                        failures.ForEach(x =>
                        {
                            if (!errorDictionary.ContainsKey(x.PropertyName))
                                errorDictionary.Add(x.PropertyName, new List<string>());

                            errorDictionary[x.PropertyName].Add(x.ErrorMessage);
                        });

                        // var options = new JsonSerializerOptions
                        // {
                        //     Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(UnicodeRanges.All)
                        // };

                        // var errorStr = JsonSerializer.Serialize(errorDictionary, options);

                        throw new GirvsException("One or more validation errors occurred.",
                            StatusCodes.Status400BadRequest, errorDictionary);
                    }
                }
            }

            return await next();
        }
    }
}