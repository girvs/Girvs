namespace Girvs.EventBus.CapEventBus;

public class GirvsCapFilter : ISubscribeFilter
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _serviceProvider;
    private IDisposable _principalScope;
    private IDisposable _serviceProviderScope;

    public GirvsCapFilter(
        [NotNull] ILogger<GirvsCapFilter> logger,
        [NotNull] IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider
            ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public Task OnSubscribeExecutingAsync(ExecutingContext context)
    {
        _logger.LogInformation(
            $"^^^^^^^^^^^^^^^^^^^^^^^^^{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}###订阅收到消息：{context.DeliverMessage.Headers["cap-msg-name"]}^^^^^^^^^^^^^^^^^^^^^^^^^^");
        _logger.LogInformation(
            $"^^^^^^^^^^^^^^^^^^^^^^^^^^开始处理订阅的消息:{context.DeliverMessage.Headers["cap-corr-id"]}^^^^^^^^^^^^^^^^^^^^^^^^^^");

        if (!IsGirvsIntegrationEventHandler(context.ConsumerDescriptor.ImplTypeInfo?.AsType()))
            return Task.CompletedTask;

        try
        {
            _serviceProviderScope = EngineContext.Current
                .ChangeCurrentThreadServiceProvider(_serviceProvider);
            var accessor = _serviceProvider.GetRequiredService<IGirvsPrincipalAccessor>();
            var principal = Identity.IntegrationEventPrincipalFactory.Create(
                context.DeliverMessage.Headers);
            _principalScope = accessor.Change(principal);
        }
        catch
        {
            RestoreContext();
            throw;
        }

        return Task.CompletedTask;
    }

    private static bool IsGirvsIntegrationEventHandler(Type type)
    {
        while (type != null && type != typeof(object))
        {
            if (type.IsGenericType
                && type.GetGenericTypeDefinition() == typeof(GirvsIntegrationEventHandler<>))
            {
                return true;
            }

            type = type.BaseType;
        }

        return false;
    }

    public Task OnSubscribeExecutedAsync(ExecutedContext context)
    {
        RestoreContext();
        _logger.LogInformation(
            $"^^^^^^^^^^^^^^^^^^^^^^^^^^{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}###订阅收到消息处理结束  Name:{context.DeliverMessage.Headers["cap-msg-name"]} Id:{context.DeliverMessage.Headers["cap-corr-id"]}");
        return Task.CompletedTask;
    }

    public Task OnSubscribeExceptionAsync(ExceptionContext context)
    {
        RestoreContext();
        _logger.LogInformation(
            $"^^^^^^^^^^^^^^^^^^^^^^^^^^{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}###订阅收到消息处理出现异常Name:{context.DeliverMessage.Headers["cap-msg-name"]} Id:{context.DeliverMessage.Headers["cap-corr-id"]}^^^^^^^^^^^^^^^^^^^^^^^^^^");
        _logger.LogError(context.Exception, context.Exception.Message);
        return Task.CompletedTask;
    }

    private void RestoreContext()
    {
        Interlocked.Exchange(ref _principalScope, null)?.Dispose();
        Interlocked.Exchange(ref _serviceProviderScope, null)?.Dispose();
    }
}
