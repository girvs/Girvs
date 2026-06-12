namespace Girvs.Cache.Caching;

public static class CachingExtensions
{
    /// <summary>
    /// Get a cached item. If it's not in the cache yet, then load and cache it.
    /// NOTE: this method is only kept for backwards compatibility: the async overload is preferred!
    /// </summary>
    /// <typeparam name="T">Type of cached item</typeparam>
    /// <param name="cacheManager">Cache manager</param>
    /// <param name="key">Cache key</param>
    /// <param name="acquire">Function to load item if it's not in the cache yet</param>
    /// <returns>The cached value associated with the specified key</returns>
    [Obsolete("同步包装会阻塞线程，请改用 IStaticCacheManager.GetAsync")]
    public static T Get<T>(this IStaticCacheManager cacheManager, CacheKey key, Func<T> acquire)
    {
        return cacheManager.GetAsync(key, acquire).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Remove items by cache key prefix
    /// </summary>
    /// <param name="cacheManager">Cache manager</param>
    /// <param name="prefix">Cache key prefix</param>
    /// <param name="prefixParameters">Parameters to create cache key prefix</param>
    [Obsolete("同步包装会阻塞线程，请改用 IStaticCacheManager.RemoveByPrefixAsync")]
    public static void RemoveByPrefix(
        this IStaticCacheManager cacheManager,
        string prefix,
        params object[] prefixParameters
    )
    {
        // GetAwaiter().GetResult() 替代 Wait()，异常不被 AggregateException 包装
        cacheManager.RemoveByPrefixAsync(prefix, prefixParameters).GetAwaiter().GetResult();
    }
}
