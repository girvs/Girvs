using Girvs.Cache.Configuration;
using Girvs.Configuration;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace Girvs.Cache.Memory
{
    public class MemoryCacheManager : IStaticCacheManager
    {
        // Flag: Has Dispose already been called?
        private bool _disposed;
        private readonly MemoryCacheConfig memoryCacheConfig;
        private readonly AppSettings appSettings;
        private readonly IMemoryCache memoryCache;

        public MemoryCacheManager(AppSettings appSettings, IMemoryCache memoryCache)
        {
            this.appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            this.memoryCache = memoryCache;
            var _cacheConfig = appSettings.ModuleConfigurations[nameof(CacheConfig)] as CacheConfig ?? throw new System.ArgumentException($"'{nameof(CacheConfig)}' cannot be null", nameof(CacheConfig));
            memoryCacheConfig = _cacheConfig.MemoryCacheConfig ?? throw new System.ArgumentException($"'{nameof(MemoryCacheConfig)}' cannot be null", nameof(MemoryCacheConfig));
        }

        #region Utilities
        /// <summary>
        /// Prepare cache entry options for the passed key
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <returns>Cache entry options</returns>
        private MemoryCacheEntryOptions PrepareEntryOptions(CacheKey key)
        {
            //set expiration time for the passed cache key
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(key.CacheTime)
            };
            return options;
        }
        /// <summary>
        /// Remove the value with the specified key from the cache
        /// </summary>
        /// <param name="cacheKey">Cache key</param>
        /// <param name="cacheKeyParameters">Parameters to create cache key</param>
        private void Remove(CacheKey cacheKey)
        {
            memoryCache.Remove(cacheKey.Key);
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

            memoryCache.Set(key.Key, data, PrepareEntryOptions(key));
        }

        #endregion

        public Task ClearAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Dispose cache manager
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
                memoryCache.Dispose();

            _disposed = true;
        }

        public T Get<T>(CacheKey key, Func<T> acquire)
        {
            if ((key?.CacheTime ?? 0) <= 0)
                return acquire();

            if (memoryCache.TryGetValue(key.Key, out T result))
                return result;

            result = acquire();

            if (result != null)
                Set(key, result);

            return result;
        }

        public async Task<T> GetAsync<T>(CacheKey key, Func<Task<T>> acquire)
        {
            if ((key?.CacheTime ?? 0) <= 0)
                return await acquire();

            if (memoryCache.TryGetValue(key.Key, out T result))
                return result;

            result = await acquire();

            if (result != null)
                await SetAsync(key, result);

            return result;
        }

        public async Task<T> GetAsync<T>(CacheKey key, Func<T> acquire)
        {
            if ((key?.CacheTime ?? 0) <= 0)
                return acquire();

            var result = memoryCache.GetOrCreate(key.Key, entry =>
            {
                entry.SetOptions(PrepareEntryOptions(key));

                return acquire();
            });

            //do not cache null value
            if (result == null)
                await RemoveAsync(key);

            return result;
        }

        public Task RemoveAsync(CacheKey cacheKey)
        {
            Remove(cacheKey);
            return Task.CompletedTask;
        }

        public Task RemoveByPrefixAsync(string prefix)
        {
            throw new NotImplementedException();
        }

        public Task SetAsync(CacheKey key, object data)
        {
            Set(key, data);

            return Task.CompletedTask;
        }
    }
}