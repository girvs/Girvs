namespace Girvs.EntityFrameworkCore.Repositories;

public class Repository<TEntity> : Repository<TEntity, Guid>, IRepository<TEntity> where TEntity : class, Entity<Guid>
{
}

public class Repository<TEntity, TKey> : IRepository<TEntity, TKey> where TEntity : class, Entity<TKey>
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
        DbContext = related.GetInstance() ??
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
        // 直接访问接口属性，避免每次校验都走反射查找。
        if (entity is not IIncludeMultiTenant<TKey> multiTenant) return true;
        var tenantId = EngineContext.Current.PrincipalAccessor.Principal.GetTenantId();

        return multiTenant.TenantId is not null && multiTenant.TenantId.ToString() == tenantId;
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
        ArgumentNullException.ThrowIfNull(t);
        if (!CompareTenantId(t))
        {
            throw new GirvsException(ShareDataOperateErrorMessage, 568);
        }

        return UpdateEntity(t, ValidateUpdateFields(fields));
    }

    public virtual Task UpdateRangeAsync(List<TEntity> ts, params string[] fields)
    {
        ArgumentNullException.ThrowIfNull(ts);
        if (ts.Any(entity => !CompareTenantId(entity)))
        {
            throw new GirvsException(ShareDataOperateErrorMessage, 568);
        }

        var properties = ValidateUpdateFields(fields);
        // 整批先校验，避免后面的非法字段值导致前面实体已被挂接或修改。
        foreach (var entity in ts)
        {
            ValidateUpdateValues(entity, properties);
            ValidateUpdateState(DbContext.Entry(entity).State);
        }
        foreach (var entity in ts)
        {
            UpdateEntity(entity, properties);
        }
        return Task.CompletedTask;
    }

    public virtual Task UpdateRangeAsync(
        Expression<Func<TEntity, bool>> predicate,
        params KeyValuePair<string, object>[] fieldValue
    )
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(fieldValue);
        if (fieldValue.Length == 0) return Task.CompletedTask;

        return Queryable.Where(predicate).ExecuteUpdateFieldsAsync(DbContext, fieldValue);
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
        ArgumentNullException.ThrowIfNull(predicate);
        DbContext.SwitchReadWriteDataBase(DataBaseWriteAndRead.Write);
        return Queryable.Where(predicate).ExecuteDeleteAsync();
    }

    public virtual Task<TEntity> GetByIdAsync(TKey id)
    {
        return Queryable.FirstOrDefaultAsync(t => t.Id.Equals(id));
    }

    public virtual Task<List<TEntity>> GetAllAsync(params string[] fields)
    {
        return fields.Length > 0
            ? Queryable.SelectProperties(fields).ToListAsync()
            : Queryable.ToListAsync();
    }

    public virtual Task<List<TEntity>> GetWhereAsync(Expression<Func<TEntity, bool>> predicate,
        params string[] fields)
    {
        return fields.Length > 0
            ? Queryable.Where(predicate).SelectProperties(fields).ToListAsync()
            : Queryable.Where(predicate).ToListAsync();
    }

    public virtual Task<List<TEntity>> GetByQueryAsync(QueryBase<TEntity> query) =>
        GetByQueryAsync(query, CancellationToken.None);

    public virtual async Task<List<TEntity>> GetByQueryAsync(QueryBase<TEntity> query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        ValidatePageSize(query.PageSize, query.MaxPageSize);
        if (query.PageIndex < 1 || (long)(query.PageIndex - 1) * query.PageSize > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(query.PageIndex));
        var queryCondition = OtherQueryCondition.And(query.GetQueryWhere());
        var source = DbSet.Where(queryCondition);
        query.RecordCount = query.IncludeRecordCount ? await source.CountAsync(cancellationToken) : -1;
        query.HasMore = false;
        if (query.IncludeRecordCount && query.PageStart >= query.RecordCount)
        {
            query.Result = [];
        }
        else
        {
            if (query.AsNoTracking) source = source.AsNoTracking();
            // 排序基于完整实体，防止 QueryFields 未包含排序字段时无法翻译；主键作为唯一排序兜底。
            var ordered = string.IsNullOrEmpty(query.OrderBy) || query.OrderBy == nameof(Entity<TKey>.Id)
                ? source.OrderByDescending(entity => entity.Id)
                : ((IOrderedQueryable<TEntity>)source.OrderByDescending(query.OrderBy)).ThenByDescending(entity => entity.Id);
            query.Result = await ordered.Skip(query.PageStart)
                .Take(query.IncludeRecordCount ? query.PageSize : query.PageSize + 1)
                .SelectProperties(query.QueryFields).ToListAsync(cancellationToken);
            query.HasMore = query.IncludeRecordCount
                ? (long)query.PageStart + query.Result.Count < query.RecordCount
                : query.Result.Count > query.PageSize;
            if (!query.IncludeRecordCount && query.HasMore) query.Result.RemoveAt(query.PageSize);
        }

        return query.Result;
    }

    private IProperty[] ValidateUpdateFields(string[] fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        if (fields.Length == 0) return [];
        var entityType = DbContext.Model.FindEntityType(typeof(TEntity));
        var names = new HashSet<string>(StringComparer.Ordinal);
        var tenantEntity = typeof(TEntity).GetInterfaces().Any(type => type.IsGenericType
            && type.GetGenericTypeDefinition() == typeof(IIncludeMultiTenant<>));
        return fields.Select(name =>
        {
            if (string.IsNullOrWhiteSpace(name) || !names.Add(name))
                throw new GirvsException("更新字段名不能为空或重复");
            var property = entityType?.FindProperty(name)
                ?? throw new GirvsException($"字段 {name} 不是已映射的标量属性");
            if (property.IsKey() || property.GetAfterSaveBehavior() != PropertySaveBehavior.Save
                || property.IsShadowProperty() || property.PropertyInfo == null
                || (tenantEntity && name == nameof(IIncludeMultiTenant<object>.TenantId)))
                throw new GirvsException($"字段 {name} 不允许指定更新");
            return property;
        }).ToArray();
    }

    private static void ValidateUpdateValues(TEntity entity, IProperty[] properties)
    {
        ArgumentNullException.ThrowIfNull(entity);
        foreach (var property in properties)
            if (!property.IsNullable && property.PropertyInfo!.GetValue(entity) == null)
                throw new GirvsException($"字段 {property.Name} 不允许为空");
    }

    private Task UpdateEntity(TEntity t, IProperty[] properties)
    {
        ValidateUpdateValues(t, properties);
        var entry = DbContext.Entry(t);
        ValidateUpdateState(entry.State);
        if (properties.Length == 0)
        {
            // 已跟踪实体保留原始值与实际变更；未跟踪且未指定字段时保留旧的全量更新语义。
            if (entry.State == EntityState.Detached) DbSet.Update(t);
        }
        else
        {
            if (entry.State == EntityState.Detached) entry.State = EntityState.Unchanged;
            foreach (var property in properties) entry.Property(property.Name).IsModified = true;
        }
        if (t is IIncludeUpdateTime updateTimeEntity)
        {
            updateTimeEntity.UpdateTime = DateTime.Now;
            if (entry.Metadata.FindProperty(nameof(IIncludeUpdateTime.UpdateTime)) is { } property
                && property.GetAfterSaveBehavior() == PropertySaveBehavior.Save)
                entry.Property(property.Name).IsModified = true;
        }
        return Task.CompletedTask;
    }

    private static void ValidateUpdateState(EntityState state)
    {
        if (state is EntityState.Added or EntityState.Deleted)
            throw new GirvsException("新增或删除状态的实体不能执行更新");
    }

    private static void ValidatePageSize(int pageSize, int? maximum)
    {
        if (pageSize < 1 || pageSize == int.MaxValue || (maximum.HasValue && (maximum.Value < 1 || pageSize > maximum.Value)))
            throw new ArgumentOutOfRangeException(nameof(pageSize), "每页条数必须为正数且不超过配置的上限");
    }

    /// <summary>按调用方提供的复合游标条件读取下一批，只读、不计总数；after 须与排序列和主键方向一致。</summary>
    public virtual async Task<QuerySlice<TEntity>> GetSliceAsync<TOrder>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TOrder>> orderBy,
        Expression<Func<TEntity, bool>> after = null,
        int pageSize = 20,
        bool descending = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(orderBy);
        cancellationToken.ThrowIfCancellationRequested();
        ValidatePageSize(pageSize, 1000);
        var source = Queryable.AsNoTracking().Where(predicate);
        if (after != null) source = source.Where(after);
        var ordered = descending
            ? source.OrderByDescending(orderBy).ThenByDescending(entity => entity.Id)
            : source.OrderBy(orderBy).ThenBy(entity => entity.Id);
        var items = await ordered.Take(pageSize + 1).ToListAsync(cancellationToken);
        var hasMore = items.Count > pageSize;
        if (hasMore) items.RemoveAt(pageSize);
        return new QuerySlice<TEntity>(items, hasMore);
    }

    /// <summary>只读流式读取，不缓冲整个结果集；枚举完成前不得释放仓储所在作用域或并行使用上下文。</summary>
    public virtual async IAsyncEnumerable<TEntity> StreamAsync(Expression<Func<TEntity, bool>> predicate,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        await foreach (var entity in Queryable.AsNoTracking().Where(predicate).AsAsyncEnumerable()
            .WithCancellation(cancellationToken))
            yield return entity;
    }

    public virtual Task<bool> ExistEntityAsync(TKey id)
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
        return Task.FromResult(DbContext.IsWasTrack<TEntity, TKey>(entity));
    }

    public Task<bool> DetachById(TKey key)
    {
        DbContext.DetachById<TEntity, TKey>(key);
        return Task.FromResult(true);
    }

    public object Entry(TEntity entity)
    {
        return DbContext.Entry(entity);
    }
}
