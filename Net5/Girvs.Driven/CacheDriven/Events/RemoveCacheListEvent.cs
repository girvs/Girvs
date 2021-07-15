using Girvs.Cache.Caching;
using Girvs.Driven.Events;

namespace Girvs.Driven.CacheDriven.Events
{
    public class RemoveCacheListEvent : Event
    {
        public RemoveCacheListEvent(CacheKey prefixKey)
        {
            PrefixKey = prefixKey;
        }

        public CacheKey PrefixKey { get; private set; }
    }
}