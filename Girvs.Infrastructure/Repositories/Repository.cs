using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Girvs.Domain.Extensions;
using Girvs.Domain.Infrastructure;
using Girvs.Domain.IRepositories;
using Girvs.Domain.Managers;
using Girvs.Domain.Models;
using Girvs.Domain.TypeFinder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Girvs.Infrastructure.Repositories
{
    public class Repository<TEntity> : Repository<TEntity, Guid>, IRepository<TEntity> where TEntity : BaseEntity<Guid>
    {
    }

    public class Repository<TEntity, Tkey> : IRepository<TEntity, Tkey> where TEntity : BaseEntity<Tkey>
    {
        private readonly ILogger<Repository<TEntity, Tkey>> _logger;
        protected DbContext DbContext { get; }
        protected DbSet<TEntity> DbSet { get; }

        protected Repository()
        {
            _logger = EngineContext.Current.Resolve<ILogger<Repository<TEntity, Tkey>>>() ??
                      throw new ArgumentNullException(nameof(Microsoft.EntityFrameworkCore.DbContext));
            DbContext = GetRelatedDbContext() ??
                        throw new ArgumentNullException(nameof(Microsoft.EntityFrameworkCore.DbContext));
            DbSet = DbContext.Set<TEntity>();
        }

        private DbContext GetRelatedDbContext()
        {
            var typeFinder = EngineContext.Current.Resolve<ITypeFinder>();
            var ts = typeFinder.FindClassesOfType(typeof(IDbContext), true, false);
            return
                (from type in ts
                    where type.GetProperties().Any(x => x.PropertyType == typeof(DbSet<TEntity>))
                    select EngineContext.Current.Resolve(type) as GirvsDbContext)
                .FirstOrDefault();
        }

        public virtual async Task<TEntity> AddAsync(TEntity t)
        {
            return (await DbSet.AddAsync(t)).Entity;
        }

        public virtual async Task<List<TEntity>> AddRangeAsync(List<TEntity> ts)
        {
            await DbSet.AddRangeAsync(ts);
            return ts;
        }

        public virtual Task UpdateAsync(TEntity t, params string[] fields)
        {
            return UpdateEntity(t, fields);
        }

        public virtual async Task UpdateRangeAsync(List<TEntity> ts, params string[] fields)
        {
            foreach (var entity in ts)
            {
                await UpdateEntity(entity, fields);
            }
        }

        public virtual Task DeleteAsync(TEntity t)
        {
            DbSet.Remove(t);
            return Task.CompletedTask;
        }

        public virtual Task DeleteRangeAsync(List<TEntity> ts)
        {
            DbSet.RemoveRange(ts);
            return Task.CompletedTask;
        }

        public virtual async Task<TEntity> GetByIdAsync(Tkey id)
        {
            return await DbSet.FindAsync(id);
        }

        public virtual Task<List<TEntity>> GetAllAsync(params string[] fields)
        {
            if (fields != null && fields.Any())
            {
                //临时方法，待改进,不科学的方法
                return Task.Run(() =>
                    DbSet.SelectProperties(fields).ToList());
            }
            else
            {
                return DbSet.ToListAsync();
            }
        }

        public virtual Task<List<TEntity>> GetWhereAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return DbSet.Where(predicate).ToListAsync();
        }

        public virtual async Task<List<TEntity>> GetByQueryAsync(QueryBase<TEntity> query)
        {
            query.RecordCount = await DbSet.Where(query.GetQueryWhere()).CountAsync();
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
                                .Where(query.GetQueryWhere())
                                .SelectProperties(query.QueryFields)
                                .OrderByDescending(query.OrderBy) //暂时取消排序
                                .Skip(query.PageStart)
                                .Take(query.PageSize)
                                .ToList());
                }
                else
                {
                    query.Result = await DbSet
                        .Where(query.GetQueryWhere())
                        .OrderByDescending(query.OrderBy) //暂时取消排序
                        .Skip(query.PageStart)
                        .Take(query.PageSize)
                        .ToListAsync();
                }
            }

            return query.Result;
        }

        /// <summary>
        /// 此方法暂时方法，不科学
        /// </summary>
        /// <param name="t">泛型T实例</param>
        /// <param name="fields">指定更新的字段</param>
        private Task UpdateEntity(TEntity t, string[] fields)
        {
            if (t is IIncludeUpdateTime updateTimeEntity)
            {
                updateTimeEntity.UpdateTime = DateTime.Now;
            }

            DbSet.Update(t);
            return Task.CompletedTask;
        }

        public virtual Task<bool> ExistEntityAsync(Tkey id)
        {
            return DbSet.AnyAsync(x => x.Id.Equals(id));
        }

        public virtual Task<bool> ExistEntityAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return DbSet.AnyAsync(predicate);
        }
    }
}