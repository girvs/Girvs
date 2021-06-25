using Dapr.Client;
using Girvs.Cache.Configuration;
using Girvs.Configuration;
using Girvs.Extensions;
using System;
using System.Threading.Tasks;

namespace Girvs.Cache.Dapr
{
    public class DaprCacheManager : IStaticCacheManager
    {
        private readonly DaprCacheConfig daprCacheConfig;
        private readonly AppSettings appSettings;
        private readonly DaprClient daprClient;

        public DaprCacheManager(AppSettings appSettings, DaprClient daprClient)
        {
            this.appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            var _cacheConfig = appSettings.ModuleConfigurations[nameof(CacheConfig)] as CacheConfig ?? throw new System.ArgumentException($"'{nameof(CacheConfig)}' cannot be null", nameof(CacheConfig));
            daprCacheConfig = _cacheConfig.DaprCacheConfig ?? throw new System.ArgumentException($"'{nameof(DaprCacheConfig)}' cannot be null", nameof(DaprCacheConfig));
            this.daprClient = daprClient;
        }

        public Task ClearAsync()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public T Get<T>(CacheKey key, Func<T> acquire)
        {
            return GetAsync<T>(key, acquire).Result;
        }

        public async Task<T> GetAsync<T>(CacheKey key, Func<Task<T>> acquire)
        {
            if (key != null && key.Key.IsNullOrWhiteSpace())
            {
                var result = await daprClient.GetStateAsync<T>(daprCacheConfig.StoreName, key.Key);
                if (result is null)
                {
                    result = await acquire();
                    if (result != null)
                    {
                        await daprClient.SaveStateAsync(daprCacheConfig.StoreName, key.Key, result);
                    }
                }
                return result;
            }
            return await acquire();
        }

        public async Task<T> GetAsync<T>(CacheKey key, Func<T> acquire)
        {
            if (key != null && key.Key.IsNullOrWhiteSpace())
            {
                var result = await daprClient.GetStateAsync<T>(daprCacheConfig.StoreName, key.Key);
                if (result is null)
                {
                    result = acquire();
                    if (result != null)
                    {
                        await daprClient.SaveStateAsync(daprCacheConfig.StoreName, key.Key, result);
                    }
                }
                return result;
            }
            return acquire();
        }

        public Task RemoveAsync(CacheKey cacheKey)
        {
            return daprClient.DeleteStateAsync(daprCacheConfig.StoreName, cacheKey.Key);
        }

        public Task RemoveByPrefixAsync(string prefix)
        {
            throw new NotImplementedException();
        }

        public Task SetAsync(CacheKey key, object data)
        {
            return daprClient.SaveStateAsync(daprCacheConfig.StoreName, key.Key, data);
        }
    }
}