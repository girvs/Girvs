﻿using System;
using System.Linq;
using System.Threading.Tasks;
using DotNetCore.CAP;
using Girvs.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Girvs.EventBus.CapEventBus
{
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
            var headers = EngineContext.Current.ClaimManager.CurrentClaims.ToDictionary(claim => claim.Type, claim => claim.Value);

            var topicName = @event.GetType().Name;
            _logger.LogInformation("Publishing event {@Event} to.{TopicName}", @event, topicName);

            await _capPublisher.PublishAsync(topicName, (dynamic)@event, headers, @event.CancellationToken);
        }
    }
}