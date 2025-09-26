namespace Girvs.Driven.CacheDriven.EventHandlers;

public class RemoveCacheEventHandlers(IStaticCacheManager staticCacheManager)
    : INotificationHandler<RemoveCacheEvent>,
        INotificationHandler<RemoveCacheListEvent>,
        INotificationHandler<SetCacheEvent>,
        INotificationHandler<RemoveCacheByPrefixEvent>
{
    public Task Handle(RemoveCacheEvent notification, CancellationToken cancellationToken)
    {
        return staticCacheManager.RemoveAsync(notification.CacheKey);
    }

    public Task Handle(RemoveCacheListEvent notification, CancellationToken cancellationToken)
    {
        return staticCacheManager.RemoveByPrefixAsync(notification.PrefixKey.Key);
    }

    public Task Handle(SetCacheEvent notification, CancellationToken cancellationToken)
    {
        return staticCacheManager.SetAsync(notification.Key, notification.Object);
    }

    public Task Handle(RemoveCacheByPrefixEvent notification, CancellationToken cancellationToken)
    {
        return staticCacheManager.RemoveByPrefixAsync(notification.PrefixKey.Key);
    }
}
