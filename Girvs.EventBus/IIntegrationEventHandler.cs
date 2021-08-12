using System.Threading.Tasks;
using DotNetCore.CAP;
using Girvs.BusinessBasis;

namespace Girvs.EventBus
{
    public interface IIntegrationEventHandler<in TIntegrationEvent> : IIntegrationEventHandler, ICapSubscribe
        where TIntegrationEvent : IntegrationEvent
    {
        Task Handle(TIntegrationEvent @event, [FromCap]CapHeader header);
    }

    public interface IIntegrationEventHandler
    {
    }
}