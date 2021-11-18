using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP;
using Girvs.Infrastructure.GirvsServiceContext;
using JetBrains.Annotations;

namespace Girvs.EventBus
{
    public abstract class GirvsIntegrationEventHandler<TIntegrationEvent> : IIntegrationEventHandler<TIntegrationEvent>, IDisposable
        where TIntegrationEvent : IntegrationEvent
    {
        private readonly IServiceProvider _serviceProvider;

        public GirvsIntegrationEventHandler(
            [NotNull] IServiceProvider serviceProvider
        )
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            ServiceContextFactory.Create(serviceProvider);
        }

        public abstract Task Handle(TIntegrationEvent @event, CapHeader header, CancellationToken cancellationToken);

        public void Dispose()
        {
        }
    }
}