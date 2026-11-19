namespace Girvs.Driven.Behaviors;

public class ValidatorBehavior<TRequest, TResponse>(
    IMediatorHandler mediator,
    IEnumerable<IValidator<TRequest>> validators,
    ILogger<ValidatorBehavior<TRequest, TResponse>> logger
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    // 默认校验器无状态且线程安全，按封闭类型静态缓存一份实例即可，避免每次请求都新建
    private static readonly GirvsDefaultCommandValidator<TRequest> DefaultValidator = new();

    private readonly IMediatorHandler _mediator =
        mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly IEnumerable<IValidator<TRequest>> _validators =
        validators ?? throw new ArgumentNullException(nameof(validators));
    private readonly ILogger<ValidatorBehavior<TRequest, TResponse>> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("Command Validator Behavior: {RequestType}", typeof(TRequest).FullName);

        // 通过注入 IEnumerable<IValidator<TRequest>> 获取已注册校验器（为空时不报错）；
        // 仅应用 GirvsCommandValidator 派生的校验器，其余回退到默认校验器
        var validator =
            _validators.OfType<GirvsCommandValidator<TRequest>>().FirstOrDefault()
            ?? DefaultValidator;

        if (request is Command<TResponse> command)
            command.AddFluentValidationRule(validator);

        var failures = (await validator.ValidateAsync(request, cancellationToken)).Errors;

        if (failures.Count == 0)
#if NET10_0_OR_GREATER
            return await next(cancellationToken);
#else
            return await next();
#endif

        //校验错误信息通过通知的方式传递至前台,如果不启用延迟处理，而直接抛出异常
        if (validator.IsErrorMessageDelay)
        {
            foreach (var error in failures)
            {
                await _mediator.RaiseEvent(
                    new DomainNotification(error.PropertyName, error.ErrorMessage),
                    cancellationToken
                );
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

            throw new GirvsException(
                "One or more validation errors occurred.",
                StatusCodes.Status400BadRequest,
                errorDictionary
            );
        }
    }
}
