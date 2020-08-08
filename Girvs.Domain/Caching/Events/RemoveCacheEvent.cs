using Girvs.Domain.Driven.Events;

namespace Girvs.Domain.Caching.Events
{
    public class RemoveCacheEvent : Event
    {
        public RemoveCacheEvent(string cacheKey)
        {
            CacheKey = cacheKey;
        }

        public string CacheKey { get; private set; }
    }
}