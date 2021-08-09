using Girvs.Cache.Caching;
using Girvs.Driven.Events;

namespace Girvs.Driven.CacheDriven.Events
{
    public class SetCacheEvent:Event
    {
        public SetCacheEvent(dynamic o, CacheKey key, int cacheTime = 30)
        {
            Object = o;
            Key = key;
            CacheTime = cacheTime;
            key.CacheTime = cacheTime;
        }

        public CacheKey Key { get; private set; }
        public dynamic Object { get; private set; }
        public int CacheTime { get; private set; }
    }
}