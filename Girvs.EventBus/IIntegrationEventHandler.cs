namespace Girvs.EventBus;

public interface IIntegrationEventHandler<in TIntegrationEvent> : IIntegrationEventHandler, ICapSubscribe
    where TIntegrationEvent : IntegrationEvent
{
    Task Handle(TIntegrationEvent @event, CapHeader header, CancellationToken cancellationToken);
}

public interface IIntegrationEventHandler
{
}