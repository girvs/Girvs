namespace Girvs.Driven.CacheDriven.Events;

public record RemoveCacheListEvent(CacheKey PrefixKey) : Event;