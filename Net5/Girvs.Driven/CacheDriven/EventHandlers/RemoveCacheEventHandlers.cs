using System;
using System.Threading;
using System.Threading.Tasks;
using Girvs.Cache.Caching;
using Girvs.Driven.CacheDriven.Events;
using MediatR;

namespace Girvs.Driven.CacheDriven.EventHandlers
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
            _staticCacheManager.RemoveByPrefix(notification.PrefixKey.Key);
            return Task.CompletedTask;
        }

        public Task Handle(SetCacheEvent notification, CancellationToken cancellationToken)
        {
            _staticCacheManager.Set(notification.Key, notification.Object);
            return Task.CompletedTask;
        }

        public Task Handle(RemoveCacheByPrefixEvent notification, CancellationToken cancellationToken)
        {
            _staticCacheManager.RemoveByPrefix(notification.PrefixKey.Key);
            return Task.CompletedTask;
        }
    }
}