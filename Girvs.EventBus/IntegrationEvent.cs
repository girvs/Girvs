namespace Girvs.EventBus;

public record IntegrationEvent(Guid Id, DateTime CreationDate,
    [property: JsonIgnore] CancellationToken CancellationToken)
{
    public IntegrationEvent() : this(Guid.NewGuid(), DateTime.UtcNow, default(CancellationToken))
    {
    }
}