using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Girvs.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Girvs.EntityFrameworkCore.Repositories
{
    public abstract class BaseActionRepository<T> : IBaseActionRepository<T> where T : BaseEntity, new()
    {
        private readonly DbContext _dbContext;
        private readonly GirvsConfig _spConfig;
        protected DbSet<T> DbSet { get; set; }


        public BaseActionRepository(DbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _spConfig = EngineContext.Current.Resolve<GirvsConfig>();
            if (_spConfig == null) throw new ArgumentException(nameof(_spConfig));
        }

        public virtual async Task<bool> AddAsync(T t)
        {
            await DbSet.AddAsync(t);
            return await _dbContext.SaveChangesAsync() > 0;
        }

        public virtual async Task<int> AddRangeAsync(List<T> ts)
        {
            await DbSet.AddRangeAsync(ts);
            return await _dbContext.SaveChangesAsync();
        }

        public virtual async Task<bool> UpdateAsync(T t, params string[] fields)
        {
            UpdateEntity(t, fields);
            return await _dbContext.SaveChangesAsync() > 0;
        }

        public virtual async Task<int> UpdateRangeAsync(List<T> ts, params string[] fields)
        {
            foreach (var t in ts)
            {
                UpdateEntity(t, fields);
            }

            return await _dbContext.SaveChangesAsync();
        }

        public virtual async Task<bool> DeleteAsync(T t)
        {
            _dbContext.Attach(t);
            _dbContext.Entry<T>(t).State = EntityState.Deleted;
            return await _dbContext.SaveChangesAsync() > 0;
        }

        public virtual async Task<int> DeleteRangeAsync(List<T> ts)
        {
            _dbContext.AttachRange(ts);
            foreach (var t in ts)
            {
                _dbContext.Entry<T>(t).State = EntityState.Deleted;
            }

            return await _dbContext.SaveChangesAsync();
        }

        public virtual async Task<T> GetByIdAsync(Guid id)
        {
            var condition = TenantCondition.And(x => x.Id == id);
            return await DbSet.FirstOrDefaultAsync(condition);
        }

        public virtual async Task<List<T>> GetAllAsync(params string[] fields)
        {
            if (fields.Any())
            {
                //临时方法，待改进,不科学的方法
                return await Task.Run(() =>
                    DbSet.SelectProperties(fields).Where(TenantCondition).ToList());
            }
            else
            {
                return await DbSet.Where(TenantCondition).ToListAsync();
            }
        }

        public virtual async Task<List<T>> GetByQueryAsync(QueryBase<T> query)
        {
            var condition = TenantCondition.And(query.GetQueryWhere());
            query.RecordCount = await DbSet.Where(condition).CountAsync();
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
                            DbSet
                                .Where(condition)
                                .SelectProperties(query.QueryFields)
                                .OrderByDescending(query.OrderBy) //暂时取消排序
                                .Skip(query.PageStart)
                                .Take(query.PageSize)
                                .ToList());
                }
                else
                {
                    query.Result = await DbSet
                        .Where(condition)
                        .OrderByDescending(query.OrderBy) //暂时取消排序
                        .Skip(query.PageStart)
                        .Take(query.PageSize)
                        .ToListAsync();
                }
            }

            return query.Result;
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
            return await DbSet.AnyAsync(x => x.Id == id);
        }
    }
}