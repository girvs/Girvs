using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP;
using Girvs.Infrastructure.GirvsServiceContext;

namespace Girvs.EventBus
{
    public abstract class GirvsIntegrationEventHandler<TIntegrationEvent> : IIntegrationEventHandler<TIntegrationEvent>
        where TIntegrationEvent : IntegrationEvent
    {

        public GirvsIntegrationEventHandler(
            IServiceProvider serviceProvider
        )
        {
            ServiceContextFactory.Create(serviceProvider);
        }

        public abstract Task Handle(TIntegrationEvent @event, CapHeader header, CancellationToken cancellationToken);
    }
}