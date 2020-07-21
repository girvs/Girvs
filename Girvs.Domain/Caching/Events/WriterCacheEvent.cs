using Girvs.Domain.Caching.Interface;
using Girvs.Domain.Driven.Events;
using Girvs.Domain.Models;

namespace Girvs.Domain.Caching.Events
{
    public class WriterCacheEvent : Event
    {
        public WriterCacheEvent(BaseEntity entity, int cacheTime, ICacheKey cacheKey, bool isClearListCache)
        {
            Entity = entity;
            CacheTime = cacheTime;
            CacheKey = cacheKey;
            IsClearListCache = isClearListCache;
        }

        public ICacheKey CacheKey { get; private set; }

        public bool IsClearListCache { get; private set; }

        public BaseEntity Entity { get; private set; }

        public int CacheTime { get; private set; }
    }
}