using System.Threading.Tasks;
using Dapr.Client;
using Microsoft.Extensions.Logging;

namespace Girvs.EventBus.DaprEvenBus
{
    public class DaprEventBus: IEventBus
    {
        public const string DAPR_PUBSUB_NAME = "pubsub";

        private readonly DaprClient _dapr;
        private readonly ILogger<DaprEventBus> _logger;

        public DaprEventBus(DaprClient dapr, ILogger<DaprEventBus> logger)
        {
            _dapr = dapr;
            _logger = logger;
        }

        public async Task PublishAsync<TIntegrationEvent>(TIntegrationEvent @event)
            where TIntegrationEvent : IntegrationEvent
        {
            var topicName = @event.GetType().Name;
            _logger.LogInformation("Publishing event {@Event} to {PubsubName}.{TopicName}", @event, DAPR_PUBSUB_NAME, topicName);

            await _dapr.PublishEventAsync(DAPR_PUBSUB_NAME, topicName, (dynamic)@event);
        }
    }
}