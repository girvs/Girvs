using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using EasyCaching.Core;
using Girvs.Domain.Caching.Interface;

namespace Girvs.Infrastructure.CacheRepository
{
    /// <summary>
    /// 表示内存缓存管理器
    /// </summary>
    public partial class MemoryCacheManager : ILocker, IStaticCacheManager
    {
        private readonly IEasyCachingProvider _provider;

        public MemoryCacheManager(IEasyCachingProvider provider)
        {
            this._provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public async Task<T> Get<T>(string key, Func<T> acquire, int? cacheTime = null)
        {
            if (cacheTime <= 0)
                return await Task.Run(acquire);


            return _provider.Get(key, acquire, TimeSpan.FromMinutes(cacheTime ?? GirvsCachingDefaults.CacheTime))
                .Value;
        }

        public async Task<string> GetToString(string key)
        {
            return (await _provider.GetAsync<string>(key))?.Value;
        }

        public async Task<T> GetAsync<T>(string key, Func<Task<T>> acquire, int? cacheTime = null)
        {
            if (cacheTime <= 0)
                return await acquire();

            var t = await _provider.GetAsync(key, acquire, TimeSpan.FromMinutes(cacheTime ?? GirvsCachingDefaults.CacheTime));
            return t.Value;
        }

        public async Task Set(string key, object data, int? cacheTime = null)
        {
            if (!(cacheTime <= 0))
                await _provider.SetAsync(key, data, TimeSpan.FromMinutes(cacheTime ?? GirvsCachingDefaults.CacheTime));
        }

        public Task<bool> IsSet(string key)
        {
            return _provider.ExistsAsync(key);
        }

        public async Task<bool> PerformActionWithLock(string key, TimeSpan expirationTime, Action action)
        {
            if (await _provider.ExistsAsync(key))
                return false;

            try
            {
                await _provider.SetAsync(key, key, expirationTime);
                action(;
                return true;
            }
            finally
            {
                await Remove(key);
            }
        }

        public async Task Remove(string key)
        {
            await _provider.RemoveAsync(key);
        }

        public async Task RemoveByPrefix(string prefix)
        {
            await _provider.RemoveByPrefixAsync(prefix);
        }

        public async Task Clear()
        {
            await _provider.FlushAsync();
        }

        public async Task<List<string>> GetCacheKeys()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            //var entries = provider.GetType().GetField("_entries", flags)?.GetValue(provider);
            var keys = new List<string>();
            var cache = _provider.GetType().GetField("_cache", flags)?.GetValue(_provider);
            if (cache != null)
            {
                if (cache.GetType().GetField("_memory", flags)?.GetValue(cache) is IDictionary memory)
                {
                    foreach (DictionaryEntry entry in memory)
                    {
                        keys.Add(entry.Key.ToString());
                    }
                }

            }

            //var cacheItems = entries as IDictionary;
            //if (cacheItems == null) return keys;
            return await Task.FromResult(keys);
        }

        public virtual void Dispose()
        {
            //nothing special
        }
    }
}