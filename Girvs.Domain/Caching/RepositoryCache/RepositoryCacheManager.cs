using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Girvs.Domain.Caching.Interface;
using Girvs.Domain.Managers;
using Girvs.Domain.Models;

namespace Girvs.Domain.Caching.RepositoryCache
{
    public class RepositoryCacheManager<T> : IRepositoryCacheManager<T> where T : BaseEntity, new()
    {
        private readonly IStaticCacheManager _staticCacheManager;

        public RepositoryCacheManager(IStaticCacheManager staticCacheManager)
        {
            _staticCacheManager = staticCacheManager ?? throw new ArgumentNullException(nameof(staticCacheManager));
        }

        private string CacheKeyPrefix => $"{typeof(T).FullName}";
        
        private string CacheKeyListPrefix => $"{CacheKeyPrefix}:List";

        private string CacheKeyListAllPrefix => $"{CacheKeyListPrefix}:All";

        private string CacheKeyListQueryPrefix => $"{CacheKeyListPrefix}:Query";

        protected virtual string BuilderKey(Guid key) => $"{CacheKeyPrefix}:{key}";

        protected virtual async Task RemoveListCache()
        {
            string keyPrefix = $"{GirvsCachingDefaults.RedisDefaultPrefix}:{CacheKeyPrefix}";
            await Task.Run(() => { _staticCacheManager.RemoveByPrefix(keyPrefix); });
        }

        public async Task<bool> AddLinkageAsync(Func<T, Task<bool>> acquire,
            int cacheTime = GirvsCachingDefaults.CacheTime)
        {
            var entity = new T();
            if (await acquire(entity))
            {
                _staticCacheManager.Set(BuilderKey(entity.Id), entity, cacheTime);
                await RemoveListCache();
                return true;
            }

            return false;
        }

        public async Task<int> AddRangeLinkageAsync(Func<List<T>, Task<int>> acquire,
            int cacheTime = GirvsCachingDefaults.CacheTime)
        {
            var entities = new List<T>();
            var result = await acquire(entities);
            foreach (var entity in entities)
            {
                _staticCacheManager.Set(BuilderKey(entity.Id), entity, cacheTime);
            }

            if (result > 0)
            {
                await RemoveListCache();
            }

            return result;
        }

        public async Task<bool> UpdateLinkageAsync(Func<T, Task<bool>> acquire,
            int cacheTime = GirvsCachingDefaults.CacheTime)
        {
            var entity = new T();
            if (await acquire(entity))
            {
                _staticCacheManager.Set(BuilderKey(entity.Id), entity, cacheTime);
                await RemoveListCache();
                return true;
            }

            return false;
        }

        public async Task<int> UpdateRangeLinkageAsync(Func<List<T>, Task<int>> acquire,
            int cacheTime = GirvsCachingDefaults.CacheTime)
        {
            var entities = new List<T>();
            var result = await acquire(entities);
            foreach (var entity in entities)
            {
                _staticCacheManager.Set(BuilderKey(entity.Id), entity, cacheTime);
            }

            if (result > 0)
            {
                await RemoveListCache();
            }

            return result;
        }

        public async Task<bool> DeleteLinkageAsync(Func<T, Task<bool>> acquire)
        {
            var entity = new T();
            if (await acquire(entity))
            {
                _staticCacheManager.Remove(BuilderKey(entity.Id));
                await RemoveListCache();
                return true;
            }

            return false;
        }

        public async Task<int> DeleteRangeLinkageAsync(Func<List<T>, Task<int>> acquire)
        {
            var entities = new List<T>();
            var result = await acquire(entities);
            foreach (var entity in entities)
            {
                _staticCacheManager.Remove(BuilderKey(entity.Id));
            }

            if (result > 0)
            {
                await RemoveListCache();
            }

            return result;
        }

        public async Task<T> GetByIdLinkageAsync(Func<Task<T>> acquire, Guid primaryKey,
            int cacheTime = GirvsCachingDefaults.CacheTime)
        {
            return await Task.Run(() => _staticCacheManager.Get(BuilderKey(primaryKey), acquire, cacheTime));
        }

        public async Task<List<T>> GetAllLinkageAsync(Func<string[], Task<List<T>>> acquire, string[] fields,
            int cacheTime = GirvsCachingDefaults.CacheTime)
        {
            string key = fields.Any() ? $"{CacheKeyListAllPrefix}:{string.Join(',', fields)}" : CacheKeyListAllPrefix;
            return await Task.Run(() => _staticCacheManager.Get(key, async () => await acquire(fields), cacheTime));
        }

        public async Task<List<T>> GetByQueryLinkageAsync(Func<QueryBase<T>, Task<List<T>>> acquire, QueryBase<T> query,
            int cacheTime = GirvsCachingDefaults.CacheTime)
        {
            query.Result =  await Task.Run(() =>
            {
                return _staticCacheManager.Get(query.GetCacheKey(CacheKeyListQueryPrefix), () => acquire(query), cacheTime);
            });
            
            return query.Result;
        }
    }
}