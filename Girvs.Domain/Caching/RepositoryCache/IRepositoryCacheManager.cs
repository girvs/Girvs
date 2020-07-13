using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Girvs.Domain.Managers;

namespace Girvs.Domain.Caching.RepositoryCache
{
    public interface IRepositoryCacheManager<T> where T : BaseEntity, new()
    {
        Task<bool> AddLinkageAsync(Func<T, Task<bool>> acquire,
            int cacheTime = GirvsCachingDefaults.CacheTime);


        Task<int> AddRangeLinkageAsync(Func<List<T>, Task<int>> acquire,
            int cacheTime = GirvsCachingDefaults.CacheTime);


        Task<bool> UpdateLinkageAsync(Func<T, Task<bool>> acquire,
            int cacheTime = GirvsCachingDefaults.CacheTime);


        Task<int> UpdateRangeLinkageAsync(Func<List<T>, Task<int>> acquire,
            int cacheTime = GirvsCachingDefaults.CacheTime);


        Task<bool> DeleteLinkageAsync(Func<T, Task<bool>> acquire);


        Task<int> DeleteRangeLinkageAsync(Func<List<T>, Task<int>> acquire);


        Task<T> GetByIdLinkageAsync(Func<Task<T>> acquire, Guid primaryKey,
            int cacheTime = GirvsCachingDefaults.CacheTime);


        Task<List<T>> GetAllLinkageAsync(Func<string[], List<T>> acquire, string[] fields,
            int cacheTime = GirvsCachingDefaults.CacheTime);


        Task<List<T>> GetByQueryLinkageAsync(Func<QueryBase<T>, Task<List<T>>> acquire, QueryBase<T> query,
            int cacheTime = GirvsCachingDefaults.CacheTime);
    }
}