using Girvs.Domain.Driven.Events;

namespace Girvs.Domain.Caching.Events
{
    public class RemoveCacheByPrefixEvent : Event
    {
        public string PrefixKey { get; }

        public RemoveCacheByPrefixEvent(string prefixKey)
        {
            PrefixKey = prefixKey;
        }
    }
}