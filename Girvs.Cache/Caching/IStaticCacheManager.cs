﻿namespace Girvs.Cache.Caching;

/// <summary>
/// Represents a manager for caching between HTTP requests (long term caching)
/// </summary>
public partial interface IStaticCacheManager : IDisposable, ICacheKeyService
{
    /// <summary>
    /// Get a cached item. If it's not in the cache yet, then load and cache it
    /// </summary>
    /// <typeparam name="T">Type of cached item</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="acquire">Function to load item if it's not in the cache yet</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the cached value associated with the specified key
    /// </returns>
    Task<T> GetAsync<T>(CacheKey key, Func<Task<T>> acquire);

    /// <summary>
    /// Get a cached item. If it's not in the cache yet, then load and cache it
    /// </summary>
    /// <typeparam name="T">Type of cached item</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="acquire">Function to load item if it's not in the cache yet</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the cached value associated with the specified key
    /// </returns>
    Task<T> GetAsync<T>(CacheKey key, Func<T> acquire);

    /// <summary>
    /// Get a cached item. If it's not in the cache yet, return a default value
    /// </summary>
    /// <typeparam name="T">Type of cached item</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="defaultValue">A default value to return if the key is not present in the cache</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the cached value associated with the specified key, or the default value if none was found
    /// </returns>
    Task<T> GetAsync<T>(CacheKey key, T defaultValue = default);

    /// <summary>
    /// Get a cached item as an <see cref="object"/> instance, or null on a cache miss.
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the cached value associated with the specified key, or null if none was found
    /// </returns>
    Task<object> GetAsync(CacheKey key);

    /// <summary>
    /// Remove the value with the specified key from the cache
    /// </summary>
    /// <param name="cacheKey">Cache key</param>
    /// <param name="cacheKeyParameters">Parameters to create cache key</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task RemoveAsync(CacheKey cacheKey, params object[] cacheKeyParameters);

    /// <summary>
    /// Add the specified key and object to the cache
    /// </summary>
    /// <param name="key">Key of cached item</param>
    /// <param name="data">Value for caching</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task SetAsync<T>(CacheKey key, T data);

    /// <summary>
    /// Remove items by cache key prefix
    /// </summary>
    /// <param name="prefix">Cache key prefix</param>
    /// <param name="prefixParameters">Parameters to create cache key prefix</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task RemoveByPrefixAsync(string prefix, params object[] prefixParameters);

    /// <summary>
    /// Clear all cache data
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task ClearAsync();

    /// <summary>
    /// Checks if the atomic locker exists
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    Task<bool> ExistAtomicLockerAsync(string key);

    /// <summary>
    /// Sets the atomic locker
    /// </summary>
    /// <param name="key"></param>
    /// <param name="expirationTime"></param>
    /// <returns></returns>
    Task<bool> SetAtomicLockerAsync(string key, TimeSpan expirationTime);

    /// <summary>
    /// Removes the atomic locker
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    Task RemoveAtomicLockerAsync(string key);
}
