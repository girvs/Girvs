using Girvs.Cache.Caching;
using Girvs.Driven.Events;

namespace Girvs.Driven.CacheDriven.Events
{
    public class RemoveCacheEvent : Event
    {
        public RemoveCacheEvent(CacheKey cacheKey)
        {
            CacheKey = cacheKey;
        }

        public CacheKey CacheKey { get; private set; }
    }
}