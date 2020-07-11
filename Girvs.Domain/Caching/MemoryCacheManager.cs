using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using EasyCaching.Core;

namespace Girvs.Domain.Caching
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

        public T Get<T>(string key, Func<T> acquire, int? cacheTime = null)
        {
            if (cacheTime <= 0)
                return acquire();

            return _provider.Get(key, acquire, TimeSpan.FromMinutes(cacheTime ?? GirvsCachingDefaults.CacheTime))
                .Value;
        }

        public string GetToString(string key)
        {
            return _provider.Get<string>(key)?.Value;
        }

        public async Task<T> GetAsync<T>(string key, Func<Task<T>> acquire, int? cacheTime = null)
        {
            if (cacheTime <= 0)
                return await acquire();

            var t = await _provider.GetAsync(key, acquire, TimeSpan.FromMinutes(cacheTime ?? GirvsCachingDefaults.CacheTime));
            return t.Value;
        }

        public void Set(string key, object data, int? cacheTime = null)
        {
            if (cacheTime <= 0)
                return;

            _provider.Set(key, data, TimeSpan.FromMinutes(cacheTime ?? GirvsCachingDefaults.CacheTime));
        }

        public bool IsSet(string key)
        {
            return _provider.Exists(key);
        }

        public bool PerformActionWithLock(string key, TimeSpan expirationTime, Action action)
        {
            if (_provider.Exists(key))
                return false;

            try
            {
                _provider.Set(key, key, expirationTime);
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
            _provider.Remove(key);
        }

        public void RemoveByPrefix(string prefix)
        {
            _provider.RemoveByPrefix(prefix);
        }

        public void Clear()
        {
            _provider.Flush();
        }

        public List<string> GetCacheKeys()
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
            
            return keys;
        }

        public virtual void Dispose()
        {
            //nothing special
        }
    }
}