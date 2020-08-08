using Girvs.Domain.Driven.Events;

namespace Girvs.Domain.Caching.Events
{
    public class RemoveCacheListEvent : Event
    {
        public RemoveCacheListEvent(string prefixKey)
        {
            PrefixKey = prefixKey;
        }

        public string PrefixKey { get; private set; }
    }
}