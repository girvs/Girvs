using Girvs.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Girvs.Cache.Redis
{
    public class RedisCacheManager : IStaticCacheManager
    {
        private readonly AppSettings appSettings;
        private readonly IDistributedCache distributedCache;

        public RedisCacheManager(AppSettings appSettings, IDistributedCache distributedCache)
        {
            this.appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            this.distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
        }


        /// <summary>
        /// Prepare cache entry options for the passed key
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <returns>Cache entry options</returns>
        private DistributedCacheEntryOptions PrepareEntryOptions(CacheKey key)
        {
            //set expiration time for the passed cache key
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(key.CacheTime)
            };

            return options;
        }


        /// <summary>
        /// Try to get the cached item
        /// </summary>
        /// <typeparam name="T">Type of cached item</typeparam>
        /// <param name="key">Cache key</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the flag which indicate is the key exists in the cache, cached item or default value
        /// </returns>
        private async Task<(bool isSet, T item)> TryGetItemAsync<T>(CacheKey key)
        {
            var json = await distributedCache.GetStringAsync(key.Key);

            if (string.IsNullOrEmpty(json))
                return (false, default);

            var item = System.Text.Json.JsonSerializer.Deserialize<T>(json);
            //_perRequestCache.Set(key.Key, item);

            return (true, item);
        }

        /// <summary>
        /// Try to get the cached item
        /// </summary>
        /// <typeparam name="T">Type of cached item</typeparam>
        /// <param name="key">Cache key</param>
        /// <returns>Flag which indicate is the key exists in the cache, cached item or default value</returns>
        private (bool isSet, T item) TryGetItem<T>(CacheKey key)
        {
            var json = distributedCache.GetString(key.Key);

            if (string.IsNullOrEmpty(json))
                return (false, default);

            var item = System.Text.Json.JsonSerializer.Deserialize<T>(json);
            //_perRequestCache.Set(key.Key, item);

            return (true, item);
        }

        /// <summary>
        /// Add the specified key and object to the cache
        /// </summary>
        /// <param name="key">Key of cached item</param>
        /// <param name="data">Value for caching</param>
        private void Set(CacheKey key, object data)
        {
            if ((key?.CacheTime ?? 0) <= 0 || data == null)
                return;

            distributedCache.SetString(key.Key, JsonSerializer.Serialize(data), PrepareEntryOptions(key));
            //_perRequestCache.Set(key.Key, data);

            //using var _ = _locker.Lock();
            //_keys.Add(key.Key);
        }

        public Task ClearAsync()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {

        }


        public T Get<T>(CacheKey key, Func<T> acquire)
        {
            //little performance workaround here:
            //we use "PerRequestCache" to cache a loaded object in memory for the current HTTP request.
            //this way we won't connect to Redis server many times per HTTP request (e.g. each time to load a locale or setting)
            //if (_perRequestCache.IsSet(key.Key))
            //    return _perRequestCache.Get(key.Key, () => default(T));

            if (key.CacheTime <= 0)
                return acquire();

            var (isSet, item) = TryGetItem<T>(key);

            if (isSet)
                return item;

            var result = acquire();

            if (result != null)
                Set(key, result);

            return result;
        }

        public async Task<T> GetAsync<T>(CacheKey key, Func<Task<T>> acquire)
        {
            //if (_perRequestCache.IsSet(key.Key))
            //    return _perRequestCache.Get(key.Key, () => default(T));

            if (key.CacheTime <= 0)
                return await acquire();

            var (isSet, item) = await TryGetItemAsync<T>(key);

            if (isSet)
                return item;

            var result = await acquire();

            if (result != null)
                await SetAsync(key, result);

            return result;
        }

        public async Task<T> GetAsync<T>(CacheKey key, Func<T> acquire)
        {
            //little performance workaround here:
            //we use "PerRequestCache" to cache a loaded object in memory for the current HTTP request.
            //this way we won't connect to Redis server many times per HTTP request (e.g. each time to load a locale or setting)
            //if (_perRequestCache.IsSet(key.Key))
            //    return _perRequestCache.Get(key.Key, () => default(T));

            if (key.CacheTime <= 0)
                return acquire();

            var (isSet, item) = await TryGetItemAsync<T>(key);

            if (isSet)
                return item;

            var result = acquire();

            if (result != null)
                await SetAsync(key, result);

            return result;
        }

        public async Task RemoveAsync(CacheKey cacheKey)
        {
            await distributedCache.RemoveAsync(cacheKey.Key);
            //_perRequestCache.Remove(cacheKey.Key);

            //using var _ = await _locker.LockAsync();
            //_keys.Remove(cacheKey.Key);
        }

        public Task RemoveByPrefixAsync(string prefix)
        {
            //prefix = PrepareKeyPrefix(prefix, prefixParameters);
            //_perRequestCache.RemoveByPrefix(prefix);

            //using var _ = await _locker.LockAsync();

            //foreach (var key in _keys.Where(key => key.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase)).ToList())
            //{
            //    await _distributedCache.RemoveAsync(key);
            //    _keys.Remove(key);
            //}
            return Task.CompletedTask;
        }

        public async Task SetAsync(CacheKey key, object data)
        {
            if ((key?.CacheTime ?? 0) <= 0 || data == null)
                return;

            await distributedCache.SetStringAsync(key.Key, JsonSerializer.Serialize(data), PrepareEntryOptions(key));
            //_perRequestCache.Set(key.Key, data);

            //using var _ = await _locker.LockAsync();
            //_keys.Add(key.Key);
        }
    }
}