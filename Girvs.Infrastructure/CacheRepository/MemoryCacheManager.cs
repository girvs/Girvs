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
        private readonly IEasyCachingProvider provider;

        public MemoryCacheManager(IEasyCachingProvider provider)
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public T Get<T>(string key, Func<T> acquire, int? cacheTime = null)
        {
            if (cacheTime <= 0)
                return acquire();

            return provider.Get(key, acquire, TimeSpan.FromMinutes(cacheTime ?? GirvsCachingDefaults.CacheTime))
                .Value;
        }

        public string GetToString(string key)
        {
            return provider.Get<string>(key)?.Value;
        }

        public async Task<T> GetAsync<T>(string key, Func<Task<T>> acquire, int? cacheTime = null)
        {
            if (cacheTime <= 0)
                return await acquire();

            var t = await provider.GetAsync(key, acquire, TimeSpan.FromMinutes(cacheTime ?? GirvsCachingDefaults.CacheTime));
            return t.Value;
        }

        public void Set(string key, object data, int? cacheTime = null)
        {
            if (cacheTime <= 0)
                return;

            provider.Set(key, data, TimeSpan.FromMinutes(cacheTime ?? GirvsCachingDefaults.CacheTime));
        }

        public bool IsSet(string key)
        {
            return provider.Exists(key);
        }

        public bool PerformActionWithLock(string key, TimeSpan expirationTime, Action action)
        {
            if (provider.Exists(key))
                return false;

            try
            {
                provider.Set(key, key, expirationTime);
                action();
                return true;
            }
            finally
            {
                Remove(key);
            }
        }

        public void Remove(string key)
        {
            provider.Remove(key);
        }

        public void RemoveByPrefix(string prefix)
        {
            provider.RemoveByPrefix(prefix);
        }

        public void Clear()
        {
            provider.Flush();
        }

        public List<string> GetCacheKeys()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            //var entries = provider.GetType().GetField("_entries", flags)?.GetValue(provider);
            var keys = new List<string>();
            var cache = provider.GetType().GetField("_cache", flags)?.GetValue(provider);
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
            
            return keys;
        }

        public virtual void Dispose()
        {
            //nothing special
        }
    }
}