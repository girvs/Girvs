using Girvs.Domain.Driven.Events;
using Girvs.Domain.Models;

namespace Girvs.Domain.Caching.Events
{
    public class CreatedCacheEvent : Event
    {
        public CreatedCacheEvent(BaseEntity entity, int cacheTime)
        {
            Entity = entity;
            CacheTime = cacheTime;
        }

        public BaseEntity Entity { get; private set; }

        public int CacheTime { get; private set; }
    }
}