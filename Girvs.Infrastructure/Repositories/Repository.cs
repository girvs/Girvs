using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Girvs.Domain.Configuration;
using Girvs.Domain.Extensions;
using Girvs.Domain.Infrastructure;
using Girvs.Domain.IRepositories;
using Girvs.Domain.Managers;
using Girvs.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Girvs.Infrastructure.Repositories
{
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : AggregateRoot, new()
    {
        private readonly DbContext _dbContext;
        private readonly GirvsConfig _girvsConfig;
        protected DbSet<TEntity> DbSet { get; set; }


        public Repository(IUnitOfWork dbContext)
        {
            _dbContext = dbContext as DbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _girvsConfig = EngineContext.Current.Resolve<GirvsConfig>();
            if (_girvsConfig == null) throw new ArgumentException(nameof(_girvsConfig));
            DbSet = _dbContext.Set<TEntity>();
        }

        public virtual IUnitOfWork UnitOfWork => _dbContext as IUnitOfWork;

        public virtual async Task<TEntity> AddAsync(TEntity t)
        {
            return (await DbSet.AddAsync(t)).Entity;
        }

        public virtual async Task<List<TEntity>> AddRangeAsync(List<TEntity> ts)
        {
            await DbSet.AddRangeAsync(ts);
            return ts;
        }

        public virtual async Task UpdateAsync(TEntity t, params string[] fields)
        {
            await UpdateEntity(t, fields);
        }

        public virtual async Task UpdateRangeAsync(List<TEntity> ts, params string[] fields)
        {
            foreach (var entity in ts)
            {
                await UpdateEntity(entity, fields);
            }
        }

        private Task ConditionalDelete(TEntity t)
        {
            var includeInit = t as IncludeInitField;
            if (includeInit != null && includeInit.IsInitData)
                return Task.CompletedTask;
            _dbContext.Entry<TEntity>(t).State = EntityState.Deleted;
            return Task.CompletedTask;
        }

        public virtual Task DeleteAsync(TEntity t)
        {
            return ConditionalDelete(t);
        }

        public virtual Task DeleteRangeAsync(List<TEntity> ts)
        {
            ts.ForEach(async x => await ConditionalDelete(x));
            return Task.CompletedTask;
        }

        public virtual async Task<TEntity> GetByIdAsync(Guid id)
        {
            var condition = TenantCondition.And(x => x.Id == id);
            return await DbSet.FirstOrDefaultAsync(condition);
        }

        public virtual async Task<List<TEntity>> GetAllAsync(params string[] fields)
        {
            if (fields != null && fields.Any())
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

        public virtual async Task<List<TEntity>> GetByQueryAsync(QueryBase<TEntity> query)
        {
            var condition = TenantCondition.And(query.GetQueryWhere());
            query.RecordCount = await DbSet.Where(condition).CountAsync();
            if (query.RecordCount < 1)
            {
                query.Result = new List<TEntity>();
            }
            else
            {
                if (query.QueryFields != null && query.QueryFields.Any())
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

        public virtual Expression<Func<TEntity, bool>> TenantCondition
        {
            get
            {
                if (_girvsConfig.TenantEnabled && _girvsConfig.WhetherTheTenantIsInvolvedInManagement)
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
        private async Task UpdateEntity(TEntity t, string[] fields)
        {
            await Task.Run(() =>
            {
                var dbEntityEntry = _dbContext.Entry(t);
                if (fields != null && fields.Any())
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
            });
        }

        public async Task<bool> ExistEntityAsync(Guid id)
        {
            return await DbSet.AnyAsync(x => x.Id == id);
        }
    }
}