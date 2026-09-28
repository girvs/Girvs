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
        var headers = new Dictionary<string, string>();
        var identityContext = Identity.IntegrationIdentityContextSerializer.Serialize(
            _principalAccessor.Principal);
        if (!string.IsNullOrEmpty(identityContext))
        {
            headers[Identity.IntegrationIdentityContextSerializer.HeaderName] = identityContext;
        }

        var topicName = @event.GetType().Name;
        _logger.LogInformation("Publishing event {@Event} to.{TopicName}", @event, topicName);

        await _capPublisher.PublishAsync(topicName, (dynamic) @event, headers, @event.CancellationToken);
    }
}
