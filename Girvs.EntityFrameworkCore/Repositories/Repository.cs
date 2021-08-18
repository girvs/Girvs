using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Girvs.BusinessBasis.Entities;
using Girvs.BusinessBasis.Queries;
using Girvs.BusinessBasis.Repositories;
using Girvs.EntityFrameworkCore.Context;
using Girvs.EntityFrameworkCore.DbContextExtensions;
using Girvs.Extensions;
using Girvs.Infrastructure;
using Girvs.TypeFinder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Girvs.EntityFrameworkCore.Repositories
{
    public class Repository<TEntity> : Repository<TEntity, Guid>, IRepository<TEntity> where TEntity : BaseEntity<Guid>
    {
    }

    public class Repository<TEntity, Tkey> : IRepository<TEntity, Tkey> where TEntity : BaseEntity<Tkey>
    {
        private readonly ILogger<Repository<TEntity, Tkey>> _logger;
        internal DbContext DbContext { get; }
        internal DbSet<TEntity> DbSet { get; }

        /// <summary>
        /// 绕开底层条件查询
        /// </summary>
        protected bool PassOtherQueryCondition = false;

        protected IQueryable<TEntity> Queryable =>
            PassOtherQueryCondition ? DbSet.Where(x => true) : DbSet.Where(OtherQueryCondition);

        protected readonly IRepositoryOtherQueryCondition _repositoryQueryCondition;

        protected Repository()
        {
            _repositoryQueryCondition = EngineContext.Current.Resolve<IRepositoryOtherQueryCondition>();
            _logger = EngineContext.Current.Resolve<ILogger<Repository<TEntity, Tkey>>>() ??
                      throw new ArgumentNullException(nameof(Microsoft.EntityFrameworkCore.DbContext));
            DbContext = GetRelatedDbContext() ??
                        throw new ArgumentNullException(nameof(Microsoft.EntityFrameworkCore.DbContext));
            DbSet = DbContext.Set<TEntity>();
        }

        private DbContext GetRelatedDbContext()
        {
            var typeFinder = EngineContext.Current.Resolve<ITypeFinder>();
            var ts = typeFinder.FindOfType(typeof(IDbContext));
            return ts.Where(x =>
                    x.GetProperties().Any(propertyInfo => propertyInfo.PropertyType == typeof(DbSet<TEntity>)))
                .Select(x => EngineContext.Current.Resolve(x) as GirvsDbContext).FirstOrDefault();
        }

        public Expression<Func<TEntity, bool>> OtherQueryCondition => _repositoryQueryCondition != null
            ? _repositoryQueryCondition.GetOtherQueryCondition<TEntity>()
            : x => true;

        public bool CompareTenantId(TEntity entity)
        {
            if (entity is not IIncludeMultiTenant<Tkey>) return true;
            var tenantId = EngineContext.Current.ClaimManager.GetTenantId();
            var identityType = EngineContext.Current.ClaimManager.GetIdentityType();
            if (string.IsNullOrEmpty(tenantId) && identityType == IdentityType.EventMessageUser)
            {
                tenantId = Guid.Empty.ToString();
            }

            var propertyValue = CommonHelper.GetProperty(entity, nameof(IIncludeMultiTenant<Tkey>.TenantId));
            return propertyValue != null && propertyValue.ToString() == tenantId;
        }

        public virtual async Task<TEntity> AddAsync(TEntity t)
        {
            if (!CompareTenantId(t))
            {
                throw new GirvsException("当前租户与数据不一致，无法操作", 568);
            }

            return (await DbSet.AddAsync(t)).Entity;
        }

        public virtual async Task<List<TEntity>> AddRangeAsync(List<TEntity> ts)
        {
            if (ts.Any(entity => !CompareTenantId(entity)))
            {
                throw new GirvsException("当前租户与数据不一致，无法操作", 568);
            }

            await DbSet.AddRangeAsync(ts);
            return ts;
        }

        public virtual Task UpdateAsync(TEntity t, params string[] fields)
        {
            if (!CompareTenantId(t))
            {
                throw new GirvsException("当前租户与数据不一致，无法操作", 568);
            }

            return UpdateEntity(t, fields);
        }

        public virtual async Task UpdateRangeAsync(List<TEntity> ts, params string[] fields)
        {
            if (ts.Any(entity => !CompareTenantId(entity)))
            {
                throw new GirvsException("当前租户与数据不一致，无法操作", 568);
            }

            foreach (var entity in ts)
            {
                await UpdateEntity(entity, fields);
            }
        }

        public virtual Task DeleteAsync(TEntity t)
        {
            if (!CompareTenantId(t))
            {
                throw new GirvsException("当前租户与数据不一致，无法操作", 568);
            }

            DbSet.Remove(t);
            return Task.CompletedTask;
        }

        public virtual Task DeleteRangeAsync(List<TEntity> ts)
        {
            if (ts.Any(entity => !CompareTenantId(entity)))
            {
                throw new GirvsException("当前租户与数据不一致，无法操作", 568);
            }

            DbSet.RemoveRange(ts);
            return Task.CompletedTask;
        }

        public virtual Task<TEntity> GetByIdAsync(Tkey id)
        {
            return Queryable.FirstOrDefaultAsync(t => t.Id.Equals(id));
        }

        public virtual Task<List<TEntity>> GetAllAsync(params string[] fields)
        {
            if (fields != null && fields.Any())
            {
                //临时方法，待改进,不科学的方法
                return Task.Run(() =>
                    Queryable.Where(OtherQueryCondition).SelectProperties(fields).ToList());
            }
            else
            {
                return Queryable.Where(OtherQueryCondition).ToListAsync();
            }
        }

        public virtual Task<List<TEntity>> GetWhereAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return Queryable.Where(predicate).ToListAsync();
        }

        public virtual async Task<List<TEntity>> GetByQueryAsync(QueryBase<TEntity> query)
        {
            var queryCondition = OtherQueryCondition.And(query.GetQueryWhere());

            query.RecordCount = await DbSet.Where(queryCondition).CountAsync();
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
                        await DbSet
                            .Where(queryCondition)
                            .SelectProperties(query.QueryFields)
                            .OrderByDescending(query.OrderBy) //暂时取消排序
                            .Skip(query.PageStart)
                            .Take(query.PageSize)
                            .ToListAsync();
                }
                else
                {
                    query.Result = await DbSet
                        .Where(queryCondition)
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
            return ExistEntityAsync(t => t.Id.Equals(id));
        }

        public virtual Task<bool> ExistEntityAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return Queryable.AnyAsync(predicate);
        }

        public Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return Queryable.FirstOrDefaultAsync(predicate);
        }
    }
}