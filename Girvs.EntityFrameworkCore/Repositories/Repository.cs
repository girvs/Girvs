﻿using Microsoft.EntityFrameworkCore.Query;

namespace Girvs.EntityFrameworkCore.Repositories;

public class Repository<TEntity> : Repository<TEntity, Guid>, IRepository<TEntity> where TEntity : class, Entity<Guid>
{
}

public class Repository<TEntity, Tkey> : IRepository<TEntity, Tkey> where TEntity : class, Entity<Tkey>
{
    private readonly string ShareDataOperateErrorMessage = "当前租户与数据不一致，无法操作";
    internal DbContext DbContext { get; }
    internal DbSet<TEntity> DbSet { get; }

    protected IQueryable<TEntity> Queryable => DbSet.Where(OtherQueryCondition);

    protected readonly IRepositoryOtherQueryCondition _repositoryQueryCondition;

    protected Repository()
    {
        _repositoryQueryCondition = EngineContext.Current.Resolve<IRepositoryOtherQueryCondition>();
        var related = EngineContext.Current.GetShardingTableRelatedByEntity<TEntity>();
        DbContext = related.GetInstant() ??
                    throw new ArgumentNullException(nameof(Microsoft.EntityFrameworkCore.DbContext));
        DbContext.ShardingAutoMigration();
        DbSet = DbContext.Set<TEntity>();
    }

    protected IQueryable<TEntity> ExcludeOtherQueryCondition()
    {
        return DbSet;
    }

    public Expression<Func<TEntity, bool>> OtherQueryCondition => _repositoryQueryCondition != null
        ? _repositoryQueryCondition.GetOtherQueryCondition<TEntity>()
        : x => true;

    public bool CompareTenantId(TEntity entity)
    {
        if (entity is not IIncludeMultiTenant<Tkey>) return true;
        var tenantId = EngineContext.Current.ClaimManager.IdentityClaim.TenantId;
        var identityType = EngineContext.Current.ClaimManager.IdentityClaim.IdentityType;
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
            throw new GirvsException(ShareDataOperateErrorMessage, 568);
        }

        return (await DbSet.AddAsync(t)).Entity;
    }

    public virtual async Task<List<TEntity>> AddRangeAsync(List<TEntity> ts)
    {
        if (ts.Any(entity => !CompareTenantId(entity)))
        {
            throw new GirvsException(ShareDataOperateErrorMessage, 568);
        }

        await DbSet.AddRangeAsync(ts);
        return ts;
    }

    public virtual Task UpdateAsync(TEntity t, params string[] fields)
    {
        if (!CompareTenantId(t))
        {
            throw new GirvsException(ShareDataOperateErrorMessage, 568);
        }

        return UpdateEntity(t, fields);
    }

    public virtual async Task UpdateRangeAsync(List<TEntity> ts, params string[] fields)
    {
        if (ts.Any(entity => !CompareTenantId(entity)))
        {
            throw new GirvsException(ShareDataOperateErrorMessage, 568);
        }

        foreach (var entity in ts)
        {
            await UpdateEntity(entity, fields);
        }
    }

    public virtual Task UpdateRangeAsync(
        Expression<Func<TEntity, bool>> predicate,
        params KeyValuePair<string, object>[] fieldValue
    )
    {
        if (!fieldValue.Any()) return Task.CompletedTask;

        Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> setPropertyCalls = b => b;

        foreach (var keyValuePair in fieldValue)
        {
            setPropertyCalls =
                setPropertyCalls.Append(b => b.SetProperty(b => keyValuePair.Key, b => keyValuePair.Value));
        }

        return Queryable.Where(predicate).ExecuteUpdateAsync(setPropertyCalls);
    }

    public virtual Task DeleteAsync(TEntity t)
    {
        if (!CompareTenantId(t))
        {
            throw new GirvsException(ShareDataOperateErrorMessage, 568);
        }

        DbSet.Remove(t);
        return Task.CompletedTask;
    }

    public virtual Task DeleteRangeAsync(List<TEntity> ts)
    {
        if (ts.Any(entity => !CompareTenantId(entity)))
        {
            throw new GirvsException(ShareDataOperateErrorMessage, 568);
        }

        DbSet.RemoveRange(ts);
        return Task.CompletedTask;
    }

    public virtual Task DeleteRangeAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return Queryable.Where(predicate).ExecuteDeleteAsync();
    }

    public virtual Task<TEntity> GetByIdAsync(Tkey id)
    {
        return Queryable.FirstOrDefaultAsync(t => t.Id.Equals(id));
    }

    public virtual Task<List<TEntity>> GetAllAsync(params string[] fields)
    {
        return fields.Any()
            ? Queryable.SelectProperties(fields).ToListAsync()
            : Queryable.ToListAsync();
    }

    public virtual Task<List<TEntity>> GetWhereAsync(Expression<Func<TEntity, bool>> predicate,
        params string[] fields)
    {
        return fields.Any()
            ? Queryable.Where(predicate).SelectProperties(fields).ToListAsync()
            : Queryable.Where(predicate).ToListAsync();
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
            query.Result =
                await DbSet
                    .Where(queryCondition)
                    .SelectProperties(query.QueryFields)
                    .OrderByDescending(query.OrderBy)
                    .Skip(query.PageStart)
                    .Take(query.PageSize).ToListAsync();
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
        if (!CompareTenantId(t))
        {
            throw new GirvsException(ShareDataOperateErrorMessage, 568);
        }

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

    public Task<bool> IsWasTrack(TEntity entity)
    {
        return Task.FromResult(DbContext.IsWasTrack<TEntity, Tkey>(entity));
    }

    public Task<bool> DetachById(Tkey key)
    {
        DbContext.DetachById<TEntity, Tkey>(key);
        return Task.FromResult(true);
    }

    public object Entry(TEntity entity)
    {
        return DbContext.Entry(entity);
    }
}