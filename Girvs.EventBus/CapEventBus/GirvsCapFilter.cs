using System.Collections.Concurrent;

namespace Girvs.EventBus.CapEventBus;

// CAP 通过 AddSubscribeFilter 以 TryAddScoped 注册本过滤器，每条消息从其专属作用域解析全新实例
// （官方源码 v8.3.1/v10.0.2 的 ISubscribeInvoker.Default.cs 一致），实例字段为每消息私有
public class GirvsCapFilter : ISubscribeFilter
{
    // Girvs 处理器类型层级遍历结果缓存，避免每条消息重复反射
    private static readonly ConcurrentDictionary<Type, bool> GirvsHandlerTypeCache = new();
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
        var headers = context.DeliverMessage.Headers;
        _logger.LogDebug(
            "订阅消息开始处理 {MessageName} {CorrelationId}",
            GetHeader(headers, "cap-msg-name"),
            GetHeader(headers, "cap-corr-id"));

        if (!IsGirvsIntegrationEventHandler(context.ConsumerDescriptor.ImplTypeInfo?.AsType()))
            return Task.CompletedTask;

        // 身份与作用域桥接：_serviceProvider 为构造注入，即 CAP 本消息专属作用域的 provider（见类头注释）。
        // 桥接后 Handle 内经 EngineContext.Resolve 取得的 Scoped 服务（如 DbContext）落在本消息作用域，
        // 与 handler 构造注入的是同一实例；身份则由 IntegrationEventPrincipalFactory 从 girvs-identity header 恢复。
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
        // 调用点传入 ImplTypeInfo?.AsType()，可能为 null：null 不是 Girvs 处理器，直接返回 false（保持旧语义）
        if (type == null)
            return false;

        return GirvsHandlerTypeCache.GetOrAdd(
            type,
            static t =>
            {
                while (t != null && t != typeof(object))
                {
                    if (t.IsGenericType
                        && t.GetGenericTypeDefinition() == typeof(GirvsIntegrationEventHandler<>))
                    {
                        return true;
                    }

                    t = t.BaseType;
                }

                return false;
            });
    }

    public Task OnSubscribeExecutedAsync(ExecutedContext context)
    {
        RestoreContext();
        var headers = context.DeliverMessage.Headers;
        _logger.LogDebug(
            "订阅消息处理结束 {MessageName} {CorrelationId}",
            GetHeader(headers, "cap-msg-name"),
            GetHeader(headers, "cap-corr-id"));
        return Task.CompletedTask;
    }

    public Task OnSubscribeExceptionAsync(ExceptionContext context)
    {
        RestoreContext();
        var headers = context.DeliverMessage.Headers;
        _logger.LogError(
            context.Exception,
            "订阅消息处理出现异常 {MessageName} {CorrelationId}",
            GetHeader(headers, "cap-msg-name"),
            GetHeader(headers, "cap-corr-id"));
        return Task.CompletedTask;
    }

    private void RestoreContext()
    {
        // Executing/Executed/Exception 在同一条消息内顺序调用，无并发竞争，直接判空还原即可
        _principalScope?.Dispose();
        _principalScope = null;
        _serviceProviderScope?.Dispose();
        _serviceProviderScope = null;
    }

    private static string GetHeader(IDictionary<string, string> headers, string key) =>
        headers.TryGetValue(key, out var value) ? value : "(unknown)";
}
