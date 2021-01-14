using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Girvs.Domain.Extensions;
using Girvs.Domain.IRepositories;
using Girvs.Domain.Managers;
using Girvs.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Girvs.Infrastructure.Repositories
{
    public class Repository<TEntity> : Repository<TEntity, Guid>, IRepository<TEntity> where TEntity : BaseEntity<Guid>
    {
        public Repository(IDbContext dbContext) : base(dbContext)
        {
        }
    }

    public class Repository<TEntity, Tkey> : IRepository<TEntity, Tkey> where TEntity : BaseEntity<Tkey>
    {
        protected DbSet<TEntity> DbSet { get; set; }


        public Repository(IDbContext dbContext)
        {
            DbSet = dbContext.Set<TEntity>();
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

        public virtual async Task<List<TEntity>> GetAllAsync(params string[] fields)
        {
            if (fields != null && fields.Any())
            {
                //临时方法，待改进,不科学的方法
                return await Task.Run(() =>
                    DbSet.SelectProperties(fields).ToList());
            }
            else
            {
                return await DbSet.ToListAsync();
            }
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

        public virtual async Task<bool> ExistEntityAsync(Tkey id)
        {
            return await DbSet.AnyAsync(x => x.Id.Equals(id));
        }


        public virtual async Task<bool> ExistEntityAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await DbSet.AnyAsync(predicate);
        }
    }
}