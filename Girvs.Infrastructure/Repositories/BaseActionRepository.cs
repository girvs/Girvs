using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Girvs.Domain;
using Girvs.Domain.Caching;
using Girvs.Domain.Configuration;
using Girvs.Domain.Extensions;
using Girvs.Domain.Infrastructure;
using Girvs.Domain.IRepositories;
using Girvs.Domain.Managers;
using Microsoft.EntityFrameworkCore;

namespace Girvs.Infrastructure.Repositories
{
    public abstract class BaseActionRepository<T> : IBaseActionRepository<T> where T : BaseEntity, new()
    {
        private readonly DbContext _dbContext;
        private readonly ICacheUsingManager _cacheUsingManager;
        private readonly GirvsConfig _spConfig;
        protected DbSet<T> CurrentDataTable { get; set; }



        public BaseActionRepository(DbContext dbContext, ICacheUsingManager cacheUsingManager)
        {
            this._dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _cacheUsingManager = cacheUsingManager ?? throw new ArgumentNullException(nameof(cacheUsingManager));
            _spConfig = EngineContext.Current.Resolve<GirvsConfig>();
            if (_spConfig == null) throw new ArgumentException(nameof(_spConfig));
        }

        public static string CacheKeyPrefix => $"{typeof(T).FullName}";

        public static string CacheKeyListPrefix => $"{CacheKeyPrefix}:List";

        public static string CacheKeyListAllPrefix => $"{CacheKeyListPrefix}:All";

        public static string CacheKeyListQueryPrefix => $"{CacheKeyListPrefix}:Query";

        public virtual string BuilderKey(string key) => $"{CacheKeyPrefix}:{key}";

        public virtual async Task RemoveListCache()
        {
            await _cacheUsingManager.ReMoveByPrefixAsync(CacheKeyListPrefix);
        }

        public virtual async Task<bool> AddAsync(T t, bool useCache = true, int? cacheTime = null)
        {
            await CurrentDataTable.AddAsync(t);
            var result = await _dbContext.SaveChangesAsync() > 0;
            await _cacheUsingManager.SetAsync(action =>
            {
                action.UseCache = useCache;
                action.CacheKey = BuilderKey(t.Id.ToString());
                action.CacheTime = cacheTime ?? GirvsCachingDefaults.CacheTime;
            }, t);

            await RemoveListCache();
            return result;
        }

        public virtual async Task<int> AddRangeAsync(List<T> ts, bool useCache = true, int? cacheTime = null)
        {
            await CurrentDataTable.AddRangeAsync(ts);
            var result = await _dbContext.SaveChangesAsync();
            foreach (var t in ts)
            {
                await _cacheUsingManager.SetAsync(action =>
                {
                    action.UseCache = useCache;
                    action.CacheKey = BuilderKey(t.Id.ToString());
                    action.CacheTime = cacheTime ?? GirvsCachingDefaults.CacheTime;
                }, t);
            }
            await RemoveListCache();
            return result;
        }

        public virtual async Task<bool> UpdateAsync(T t, bool useCache = true, int? cacheTime = null, params string[] fields)
        {
            UpdateEntity(t, fields);
            var result = await _dbContext.SaveChangesAsync() > 0;
            await _cacheUsingManager.SetAsync(action =>
            {
                action.UseCache = useCache;
                action.CacheKey = BuilderKey(t.Id.ToString());
                action.CacheTime = cacheTime ?? GirvsCachingDefaults.CacheTime;
            }, t);

            await RemoveListCache();
            return result;
        }

        public virtual async Task<int> UpdateRangeAsync(List<T> ts, bool useCache = true, int? cacheTime = null, params string[] fields)
        {
            foreach (var t in ts)
            {
                UpdateEntity(t, fields);
            }

            var result = await _dbContext.SaveChangesAsync();
            foreach (var t in ts)
            {

                await _cacheUsingManager.SetAsync(action =>
                {
                    action.UseCache = useCache;
                    action.CacheKey = BuilderKey(t.Id.ToString());
                    action.CacheTime = cacheTime ?? GirvsCachingDefaults.CacheTime;
                }, t);
            }

            await RemoveListCache();
            return result;
        }

        public virtual async Task<bool> DeleteAsync(T t, bool useCache = true, int? cacheTime = null)
        {
            _dbContext.Attach(t);
            _dbContext.Entry<T>(t).State = EntityState.Deleted;
            var result = await _dbContext.SaveChangesAsync() > 0;
            await _cacheUsingManager.ReMoveAsync(BuilderKey(t.Id.ToString()));
            await RemoveListCache();
            return result;
        }

        public virtual async Task<int> DeleteRangeAsync(List<T> ts, bool useCache = true, int? cacheTime = null)
        {
            _dbContext.AttachRange(ts);
            foreach (var t in ts)
            {
                _dbContext.Entry<T>(t).State = EntityState.Deleted;
            }

            var result = await _dbContext.SaveChangesAsync();

            foreach (var t in ts)
                await _cacheUsingManager.ReMoveAsync(BuilderKey(t.Id.ToString()));
            await RemoveListCache();
            return result;
        }

        public virtual async Task<T> GetByIdAsync(Guid id, bool useCache = true, int? cacheTime = null)
        {
            return await _cacheUsingManager.GetAsync(p =>
            {
                p.CacheTime = cacheTime ?? GirvsCachingDefaults.CacheTime;
                p.CacheKey = BuilderKey(id.ToString());
                p.UseCache = useCache;
            }, async () =>
            {
                var condition = TenantCondition.And(x => x.Id == id);
                return await CurrentDataTable.FirstOrDefaultAsync(condition);
            });
        }

        public virtual async Task<List<T>> GetAllAsync(bool useCache = true, int? cacheTime = null, params string[] fields)
        {
            string key = fields.Any() ? $"{CacheKeyListAllPrefix}:{string.Join(',', fields)}" : CacheKeyListAllPrefix;
            return await _cacheUsingManager.GetAsync(p =>
            {
                p.CacheTime = cacheTime ?? GirvsCachingDefaults.CacheTime;
                p.CacheKey = key;
                p.UseCache = useCache;
            }, async () =>
            {
                if (fields.Any())
                {
                    //临时方法，待改进,不科学的方法
                    return await Task.Run(() => CurrentDataTable.SelectProperties(fields).Where(TenantCondition).ToList());
                }
                else
                {
                    return await CurrentDataTable.Where(TenantCondition).ToListAsync();
                }
            });
        }

        public virtual async Task<List<T>> GetByQueryAsync(QueryBase<T> query, bool useCache = true, int? cacheTime = null)
        {
            query.Result = await _cacheUsingManager.GetAsync(p =>
            {
                p.CacheTime = cacheTime ?? GirvsCachingDefaults.CacheTime;
                p.CacheKey = query.GetCacheKey(CacheKeyListQueryPrefix);
                p.UseCache = useCache;
            }, async () =>
            {
                var condition = TenantCondition.And(query.GetQueryWhere());
                query.RecordCount = await CurrentDataTable.Where(condition).CountAsync();
                if (query.RecordCount < 1)
                {
                    query.Result = new List<T>();
                }
                else
                {
                    if (query.QueryFields.Any())
                    {
                        //临时方法，待改进,不科学的方法
                        query.Result =
                            await Task.Run(() =>
                                CurrentDataTable
                                    .Where(condition)
                                    .SelectProperties(query.QueryFields)
                                    .OrderByDescending(query.OrderBy) //暂时取消排序
                                    .Skip(query.PageStart)
                                    .Take(query.PageSize)
                                    .ToList());
                    }
                    else
                    {
                        query.Result = await CurrentDataTable
                            .Where(condition)
                            .OrderByDescending(query.OrderBy) //暂时取消排序
                            .Skip(query.PageStart)
                            .Take(query.PageSize)
                            .ToListAsync();
                    }
                }

                return query.Result;
            });
            return query.Result;
        }

        public virtual async Task<List<T>> GetByQueryActionAsync<TAt>(Action<TAt> action, bool useCache = true, int? cacheTime = null) where TAt : QueryBase<T>, new()
        {
            var query = default(TAt);
            action(query);
            return await GetByQueryAsync(query, useCache, cacheTime);
        }



        public virtual Expression<Func<T, bool>> TenantCondition
        {
            get
            {
                if (_spConfig.TenantEnabled && _spConfig.WhetherTheTenantIsInvolvedInManagement)
                {
                    return x => x.TenantId == EngineContext.Current.CurrentClaimTenantId;
                }
                else
                {
                    return x => true;
                }
            }
        }


        /// <summary>
        /// 此方法暂时方法，不科学
        /// </summary>
        /// <param name="t">泛型T实例</param>
        /// <param name="fields">指定更新的字段</param>
        private void UpdateEntity(T t, string[] fields)
        {
            var dbEntityEntry = _dbContext.Attach(t);
            if (fields.Any())
            {
                if (!fields.Contains(nameof(BaseEntity.UpdateTime)))
                {
                    dbEntityEntry.Property(nameof(BaseEntity.UpdateTime)).IsModified = true;
                }

                foreach (var property in fields)
                {
                    if (property == nameof(BaseEntity.Id) || property == nameof(BaseEntity.CreateTime) ||
                        property == nameof(BaseEntity.Creator) || property == nameof(BaseEntity.TenantId))
                    {
                        dbEntityEntry.Property(property).IsModified = false;
                    }
                    else
                    {
                        dbEntityEntry.Property(property).IsModified = true;
                    }
                }
            }
            else
            {
                dbEntityEntry.State = EntityState.Modified;
                dbEntityEntry.Property(nameof(BaseEntity.CreateTime)).IsModified = false;
                dbEntityEntry.Property(nameof(BaseEntity.Id)).IsModified = false;
                dbEntityEntry.Property(nameof(BaseEntity.Creator)).IsModified = false;
                dbEntityEntry.Property(nameof(BaseEntity.TenantId)).IsModified = false;
            }
        }

        public async Task<bool> ExistEntityAsync(Guid id)
        {
            return await CurrentDataTable.AnyAsync(x => x.Id == id);
        }
    }
}