namespace Girvs.EventBus.CapEventBus;

public class CapEventBus : IEventBus
{
    private readonly ICapPublisher _capPublisher;
    private readonly ILogger<CapEventBus> _logger;
    private readonly IGirvsPrincipalAccessor _principalAccessor;

    public CapEventBus(
        ICapPublisher capPublisher,
        ILogger<CapEventBus> logger,
        IGirvsPrincipalAccessor principalAccessor)
    {
        _capPublisher = capPublisher ?? throw new ArgumentNullException(nameof(capPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _principalAccessor = principalAccessor ?? throw new ArgumentNullException(nameof(principalAccessor));
    }

    public async Task PublishAsync<TIntegrationEvent>(TIntegrationEvent @event)
        where TIntegrationEvent : IntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(@event);

        var headers = new Dictionary<string, string>();
        // 发布端按白名单序列化当前用户身份注入 girvs-identity header，由订阅端 GirvsCapFilter 恢复；
        // identityContext 为 null（无白名单 Claim）时不注入该 header，订阅端以匿名身份执行
        var identityContext = Identity.IntegrationIdentityContextSerializer.Serialize(
            _principalAccessor.Principal);
        if (!string.IsNullOrEmpty(identityContext))
        {
            headers[Identity.IntegrationIdentityContextSerializer.HeaderName] = identityContext;
        }

        // topic 为事件类型名，须与订阅端 [CapSubscribe("...")] 字符串一致
        var topicName = @event.GetType().Name;
        _logger.LogInformation("发布集成事件 {Event} 到 {TopicName}", @event, topicName);

        await _capPublisher.PublishAsync(topicName, (dynamic) @event, headers, @event.CancellationToken);
    }
}
