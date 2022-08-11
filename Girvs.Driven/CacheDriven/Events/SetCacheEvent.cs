namespace Girvs.Driven.CacheDriven.Events;

public record SetCacheEvent(dynamic Object, CacheKey Key, int CacheTime = 30) : Event;