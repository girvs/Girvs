using System.Threading;
using System.Threading.Tasks;
using Girvs.Domain.Caching.Events;
using Girvs.Domain.Caching.Interface;
using MediatR;

namespace Girvs.Domain.Caching.EventHandlers
{
    public class CreatedCacheEventHandlers : INotificationHandler<CreatedCacheEvent>
    {
        private readonly IStaticCacheManager _staticCacheManager;

        public CreatedCacheEventHandlers(IStaticCacheManager staticCacheManager)
        {
            _staticCacheManager = staticCacheManager;
        }

        public async Task Handle(CreatedCacheEvent notification, CancellationToken cancellationToken)
        {
            await RemoveListCache(notification);
            _staticCacheManager.Set(notification.CacheEntityKey, notification.Entity, notification.CacheTime);
        }

        private async Task RemoveListCache(CreatedCacheEvent notification)
        {
            string keyPrefix = $"{GirvsCachingDefaults.RedisDefaultPrefix}:{notification.CacheKeyPrefix}";
            await Task.Run(() => { _staticCacheManager.RemoveByPrefix(keyPrefix); });
        }
    }
}