namespace Girvs.Driven.CacheDriven.Events;

public record RemoveCacheByPrefixEvent(CacheKey PrefixKey) : Event;
