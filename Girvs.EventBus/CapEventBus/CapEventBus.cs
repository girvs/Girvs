﻿namespace Girvs.EventBus.CapEventBus;

public class CapEventBus : IEventBus
{
    private readonly ICapPublisher _capPublisher;
    private readonly ILogger<CapEventBus> _logger;

    public CapEventBus(ICapPublisher capPublisher, ILogger<CapEventBus> logger)
    {
        _capPublisher = capPublisher ?? throw new ArgumentNullException(nameof(capPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task PublishAsync<TIntegrationEvent>(TIntegrationEvent @event)
        where TIntegrationEvent : IntegrationEvent
    {
        //传递身份信息头
        var headers =
            EngineContext.Current.ClaimManager.IdentityClaim?.OtherClaims ??
            new Dictionary<string, string>();

        var newHeaders = new Dictionary<string, string>(headers);

        var topicName = @event.GetType().Name;
        _logger.LogInformation("Publishing event {@Event} to.{TopicName}", @event, topicName);

        await _capPublisher.PublishAsync(topicName, (dynamic) @event, newHeaders, @event.CancellationToken);
    }
}