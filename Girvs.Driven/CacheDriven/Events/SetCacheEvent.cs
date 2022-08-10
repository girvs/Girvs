namespace Girvs.Driven.CacheDriven.Events;

public record SetCacheEvent(CacheKey Key, int CacheTime, dynamic Object) : Event;