namespace Girvs.Driven.CacheDriven.Events;

public record RemoveCacheEvent(CacheKey CacheKey) : Event;
