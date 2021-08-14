using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP;

namespace Girvs.EventBus
{
    public interface IIntegrationEventHandler<in TIntegrationEvent> : IIntegrationEventHandler, ICapSubscribe
        where TIntegrationEvent : IntegrationEvent
    {
        Task Handle(TIntegrationEvent @event, [FromCap] CapHeader header, CancellationToken cancellationToken);
    }

    public interface IIntegrationEventHandler
    {
    }
}