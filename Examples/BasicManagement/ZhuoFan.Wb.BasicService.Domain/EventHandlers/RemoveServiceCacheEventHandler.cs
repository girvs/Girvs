using System;
using System.Threading;
using System.Threading.Tasks;
using Girvs.EventBus;
using JetBrains.Annotations;
using MediatR;
using ZhuoFan.Wb.BasicService.Domain.Events;
using ZhuoFan.Wb.Common.Events.Authorize;

namespace ZhuoFan.Wb.BasicService.Domain.EventHandlers
{
    public class RemoveServiceCacheEventHandler : INotificationHandler<RemoveServiceCacheEvent>
    {
        private readonly IEventBus _eventBus;

        public RemoveServiceCacheEventHandler([NotNull] IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public async Task Handle(RemoveServiceCacheEvent notification, CancellationToken cancellationToken)
        {
            var @event = new RemoveAuthorizeCacheEvent();
            await _eventBus.PublishAsync(@event);
        }
    }
}