using System.Threading;
using System.Threading.Tasks;
using Girvs.Domain.Caching.Events;
using Girvs.Domain.Caching.Interface;
using MediatR;

namespace Girvs.Domain.Caching.EventHandlers
{
    public class WriterCacheEventHandlers : INotificationHandler<WriterCacheEvent>
    {
        private readonly IStaticCacheManager _staticCacheManager;

        public WriterCacheEventHandlers(IStaticCacheManager staticCacheManager)
        {
            _staticCacheManager = staticCacheManager;
        }

        public async Task Handle(WriterCacheEvent notification, CancellationToken cancellationToken)
        {
            if (notification.IsClearListCache)
            {
                await RemoveListCache(notification);
            }

            string cacheKey = notification.CacheKey.GetBuildEntityKey(notification.Entity.Id);
            _staticCacheManager.Set(cacheKey, notification.Entity, notification.CacheTime);
        }

        private async Task RemoveListCache(WriterCacheEvent notification)
        {
            await Task.Run(() => { _staticCacheManager.RemoveByPrefix(notification.CacheKey.CacheKeyListPrefix); });
        }
    }
}