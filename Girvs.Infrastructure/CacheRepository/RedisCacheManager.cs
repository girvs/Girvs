using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Girvs.Domain.Caching.Interface;
using Girvs.Domain.Caching.Interface.Redis;
using Girvs.Domain.Configuration;
using Girvs.Infrastructure.CacheRepository.Redis;
using StackExchange.Redis;

namespace Girvs.Infrastructure.CacheRepository
{
    /// <summary>
    ///代表Redis商店中的缓存管理器（http://redis.io/）。
    ///大多数情况下，它将在Web场或Azure中运行时使用。但当然它也可以在任何服务器或环境中使用
    /// </summary>
    public partial class RedisCacheManager : IStaticCacheManager
    {
        private readonly ICacheManager _perRequestCacheManager;
        private readonly IRedisConnectionWrapper _connectionWrapper;
        private readonly IDatabase _db;

        public RedisCacheManager(ICacheManager perRequestCacheManager,
            IRedisConnectionWrapper connectionWrapper,
            GirvsConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (string.IsNullOrEmpty(config.RedisConnectionString))
                throw new Exception("Redis connection string is empty");

            this._perRequestCacheManager = perRequestCacheManager ?? throw new ArgumentNullException(nameof(perRequestCacheManager));
            this._connectionWrapper = connectionWrapper ?? throw new ArgumentNullException(nameof(connectionWrapper));
            _db = this._connectionWrapper.GetDatabase(config.RedisDatabaseId ?? (int)RedisDatabaseNumber.Cache);
        }

        protected virtual IEnumerable<RedisKey> GetKeys(EndPoint endPoint, string prefix = null)
        {
            var server = _connectionWrapper.GetServer(endPoint);
            var keys = server.Keys(_db.Database, string.IsNullOrEmpty(prefix) ? null : $"{prefix}*");
            keys = keys.Where(key => !key.ToString().Equals(GirvsCachingDefaults.RedisDataProtectionKey, StringComparison.OrdinalIgnoreCase));
            return keys;
        }

        protected virtual async Task<T> GetAsync<T>(string key)
        {
            if (_perRequestCacheManager.IsSet(key))
                return _perRequestCacheManager.Get(key, () => default(T), 0);

            var serializedItem = await _db.StringGetAsync(key);
            if (!serializedItem.HasValue)
                return default(T);

            var item = JsonSerializer.Deserialize<T>(serializedItem);
            if (item == null)
                return default(T);

            _perRequestCacheManager.Set(key, item, 0);
            return item;
        }

        protected virtual async Task SetAsync(string key, object data, int cacheTime)
        {
            if (data == null)
                return;
            var expiresIn = TimeSpan.FromMinutes(cacheTime);
            var serializedItem = JsonSerializer.Serialize(data);
            await _db.StringSetAsync(key, serializedItem, expiresIn);
        }

        protected virtual async Task<bool> IsSetAsync(string key)
        {
            if (_perRequestCacheManager.IsSet(key))
                return true;

            return await _db.KeyExistsAsync(key);
        }

        public virtual T Get<T>(string key)
        {
            if (_perRequestCacheManager.IsSet(key))
                return _perRequestCacheManager.Get(key, () => default(T), 0);

            var serializedItem = _db.StringGet(key);
            if (!serializedItem.HasValue)
                return default(T);

            var item = JsonSerializer.Deserialize<T>(serializedItem);
            if (item == null)
                return default(T);

            _perRequestCacheManager.Set(key, item, 0);
            return item;
        }

        public virtual T Get<T>(string key, Func<T> acquire, int? cacheTime = null)
        {
            if (IsSet(key))
                return Get<T>(key);

            var result = acquire();

            if ((cacheTime ?? GirvsCachingDefaults.CacheTime) > 0)
                Set(key, result, cacheTime ?? GirvsCachingDefaults.CacheTime);

            return result;
        }

        public string GetToString(string key)
        {
            if (IsSet(key))
                return _db.StringGet(key);
            return String.Empty;
        }

        public virtual void Set(string key, object data, int? cacheTime = null)
        {
            if (data == null)
                return;

            var expiresIn = TimeSpan.FromMinutes(cacheTime ?? GirvsCachingDefaults.CacheTime);
            var serializedItem = JsonSerializer.Serialize(data);

            _db.StringSet(key, serializedItem, expiresIn);
        }

        public virtual bool IsSet(string key)
        {
            if (_perRequestCacheManager.IsSet(key))
                return true;
            return _db.KeyExists(key);
        }

        public virtual void Remove(string key)
        {
            if (key.Equals(GirvsCachingDefaults.RedisDataProtectionKey, StringComparison.OrdinalIgnoreCase))
                return;
            _db.KeyDelete(key);
            _perRequestCacheManager.Remove(key);
        }

        public virtual void RemoveByPrefix(string prefix)
        {
            _perRequestCacheManager.RemoveByPrefix(prefix);

            foreach (var endPoint in _connectionWrapper.GetEndPoints())
            {
                var keys = GetKeys(endPoint, prefix);

                _db.KeyDelete(keys.ToArray());
            }
        }

        public virtual void Clear()
        {
            foreach (var endPoint in _connectionWrapper.GetEndPoints())
            {
                var keys = GetKeys(endPoint).ToArray();
                foreach (var redisKey in keys)
                {
                    _perRequestCacheManager.Remove(redisKey.ToString());
                }
                _db.KeyDelete(keys);
            }
        }

        public virtual void Dispose()
        {
            if (_connectionWrapper != null)
                _connectionWrapper.Dispose();
        }

        public List<string> GetCacheKeys()
        {
            List<string> redisKeys = new List<string>();
            foreach (var endPoint in _connectionWrapper.GetEndPoints())
            {
                var keys = GetKeys(endPoint).ToArray();
                foreach (var redisKey in keys)
                {
                    redisKeys.Add(redisKey.ToString());
                }
            }
            return redisKeys;
        }

        public async Task<T> GetAsync<T>(string key, Func<Task<T>> acquire, int? cacheTime = null)
        {
            //项目已经在缓存中，请返回
            if (await IsSetAsync(key))
                return await GetAsync<T>(key);

            var result = await acquire();

            if ((cacheTime ?? GirvsCachingDefaults.CacheTime) > 0)
                await SetAsync(key, result, cacheTime ?? GirvsCachingDefaults.CacheTime);

            return result;
        }
    }
}