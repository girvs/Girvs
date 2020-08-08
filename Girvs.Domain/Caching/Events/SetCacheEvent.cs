using Girvs.Domain.Driven.Events;

namespace Girvs.Domain.Caching.Events
{
    public class SetCacheEvent:Event
    {
        public SetCacheEvent(dynamic o, string key, int cacheTime)
        {
            Object = o;
            Key = key;
            CacheTime = cacheTime;
        }

        public string Key { get; private set; }
        public dynamic Object { get; private set; }

        public int CacheTime { get; private set; }
    }
}