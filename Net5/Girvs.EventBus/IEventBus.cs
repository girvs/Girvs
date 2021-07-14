using System.Threading.Tasks;

namespace Girvs.EventBus
{
    public interface IEventBus
    {
        Task PublishAsync<TIntegrationEvent>(TIntegrationEvent @event)
            where TIntegrationEvent : IntegrationEvent;
    }
}