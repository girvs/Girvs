namespace Girvs.EventBus;

public abstract class GirvsIntegrationEventHandler<TIntegrationEvent> : IIntegrationEventHandler<TIntegrationEvent>,
    IDisposable
    where TIntegrationEvent : IntegrationEvent
{
    public GirvsIntegrationEventHandler(
        [NotNull] IServiceProvider serviceProvider
    )
    {
        EngineContext.Current.SetCurrentThreadServiceProvider(serviceProvider);
    }

    public abstract Task Handle(TIntegrationEvent @event, CapHeader header, CancellationToken cancellationToken);

    public virtual void Dispose()
    {
        EngineContext.Current.SetCurrentThreadServiceProvider(null);
    }
}