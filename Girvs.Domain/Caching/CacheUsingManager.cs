using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Girvs.Domain.Caching
{
    public class CacheUsingManager : ICacheUsingManager
    {
        private readonly IStaticCacheManager _staticCacheManager;

        public CacheUsingManager(IStaticCacheManager staticCacheManager)
        {
            this._staticCacheManager = staticCacheManager ?? throw new ArgumentNullException(nameof(staticCacheManager));
        }


        public async Task SetAsync(Action<ActionCacheUsingParameter> action, object data)
        {
            if (action == null || data == null) return;
            var p = new ActionCacheUsingParameter();
            action(p);
            if (GirvsCachingDefaults.DefaultUseCache
                && p.UseCache
                && !string.IsNullOrEmpty(p.CacheKey))
            {
                // 清除所有列表缓存
                if (!string.IsNullOrEmpty(p.AllListKeyPrefix))
                    await ReMoveByPrefixAsync($"{GirvsCachingDefaults.RedisDefaultPrefix}:{p.AllListKeyPrefix}");

                // 清除查询列表缓存
                if (!string.IsNullOrEmpty(p.QueryListKeyPrefix))
                    await ReMoveByPrefixAsync($"{GirvsCachingDefaults.RedisDefaultPrefix}:{p.QueryListKeyPrefix}");

                p.CacheKey = $"{GirvsCachingDefaults.RedisDefaultPrefix}:{p.CacheKey}";
                await Task.Run(() =>
                {
                    _staticCacheManager.Set(p.CacheKey, data, p.CacheTime);
                });
            }
        }

        public async Task<T> GetAsync<T>(Action<ActionCacheUsingParameter> action, Func<Task<T>> acquire)
        {
            if (acquire == null) throw new ArgumentNullException(nameof(acquire));
            var p = new ActionCacheUsingParameter();
            action(p);
            if (GirvsCachingDefaults.DefaultUseCache
                && p.UseCache
                && !string.IsNullOrEmpty(p.CacheKey))
            {
                p.CacheKey = $"{GirvsCachingDefaults.RedisDefaultPrefix}:{p.CacheKey}";
                return await _staticCacheManager.GetAsync(p.CacheKey, acquire, p.CacheTime);
            }
            else
            {
                return await acquire();
            }
        }

        public async Task ReMoveAsync(string key)
        {
            key = $"{GirvsCachingDefaults.RedisDefaultPrefix}:{key}";
            await Task.Run(() =>
            {
                _staticCacheManager.Remove(key);
            });
        }

        public async Task ReMoveByPrefixAsync(string keyPrefix)
        {
            keyPrefix = $"{GirvsCachingDefaults.RedisDefaultPrefix}:{keyPrefix}";
            await Task.Run(() =>
            {
                _staticCacheManager.RemoveByPrefix(keyPrefix);
            });
        }

        public async Task<IList<string>> GetAllKeysAsync()
        {
            return await Task.Run(() => _staticCacheManager.GetCacheKeys());
        }

        public async Task<string> GetToString(string key)
        {
            return await Task.Run(() => _staticCacheManager.GetToString(key));
        }
    }
}