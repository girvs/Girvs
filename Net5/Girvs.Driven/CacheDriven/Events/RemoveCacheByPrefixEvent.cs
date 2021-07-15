using Girvs.Cache.Caching;
using Girvs.Driven.Events;

namespace Girvs.Driven.CacheDriven.Events
{
    public class RemoveCacheByPrefixEvent : Event
    {
        public CacheKey PrefixKey { get; }

        public RemoveCacheByPrefixEvent(CacheKey prefixKey)
        {
            PrefixKey = prefixKey;
        }
    }
}