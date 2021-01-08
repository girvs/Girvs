using System;
using System.Threading;
using System.Threading.Tasks;
using Girvs.Domain.Caching.Events;
using Girvs.Domain.Caching.Interface;
using MediatR;

namespace Girvs.Domain.Caching.EventHandlers
{
    public class RemoveCacheEventHandlers :
        INotificationHandler<RemoveCacheEvent>,
        INotificationHandler<RemoveCacheListEvent>,
        INotificationHandler<SetCacheEvent>,
        INotificationHandler<RemoveCacheByPrefixEvent>
    {
        private readonly IStaticCacheManager _staticCacheManager;

        public RemoveCacheEventHandlers(IStaticCacheManager staticCacheManager)
        {
            _staticCacheManager = staticCacheManager ?? throw new ArgumentNullException(nameof(staticCacheManager));
        }

        public Task Handle(RemoveCacheEvent notification, CancellationToken cancellationToken)
        {
            _staticCacheManager.Remove(notification.CacheKey);
            return Task.CompletedTask;
        }

        public Task Handle(RemoveCacheListEvent notification, CancellationToken cancellationToken)
        {
            _staticCacheManager.RemoveByPrefix(notification.PrefixKey);
            return Task.CompletedTask;
        }

        public Task Handle(SetCacheEvent notification, CancellationToken cancellationToken)
        {
            _staticCacheManager.Set(notification.Key, notification.Object, notification.CacheTime);
            return Task.CompletedTask;
        }

        public Task Handle(RemoveCacheByPrefixEvent notification, CancellationToken cancellationToken)
        {
            _staticCacheManager.RemoveByPrefix(notification.PrefixKey);
            return Task.CompletedTask;
        }
    }
}