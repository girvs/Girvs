namespace Girvs.EntityFrameworkCore.DbContextExtensions;

public static class DbContextDetachAllExtension
{
    /// <summary>
    /// 取消跟踪DbContext中所有被跟踪的实体
    /// </summary>
    public static void DetachAll(this DbContext dbContext)
    {
        // 先快照条目再一次遍历，避免逐条重新枚举 ChangeTracker（原实现为 O(n²)）。
        foreach (var entry in dbContext.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }
    }

    public static bool IsWasTrack<TEntity, TKey>(this DbContext dbContext, TEntity t)
        where TEntity : class, Entity<TKey>
    {
        return dbContext.ChangeTracker.Entries<TEntity>().Any(x => x.Entity.Id.Equals(t.Id));
    }

    public static void DetachById<TEntity, TKey>(this DbContext dbContext, params TKey[] idStrings)
        where TEntity : class, Entity<TKey>
    {
        if (idStrings.Length == 0) return;

        var idSet = idStrings.ToHashSet();
        var currentEntries = dbContext
            .ChangeTracker.Entries<TEntity>()
            .Where(x => idSet.Contains(x.Entity.Id))
            .ToList();
        foreach (var currentEntry in currentEntries)
        {
            currentEntry.State = EntityState.Detached;
        }
    }

    public static void Detach<TEntity, TKey>(this DbContext dbContext) where TEntity : BaseEntity<TKey>
    {
        // 与 DetachAll 同理，先快照再遍历，避免 O(n²)。
        foreach (var currentEntry in dbContext.ChangeTracker.Entries<TEntity>().ToList())
        {
            currentEntry.State = EntityState.Detached;
        }
    }

    /// <summary>
    /// 取消指定更新的实体
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="entity"></param>
    /// <typeparam name="T"></typeparam>
    public static void DetachSpecifyEntity<TEntity, TKey>(this DbContext dbContext, TEntity entity)
        where TEntity : BaseEntity<TKey>
    {
        //每次循环获取DbContext中一个被跟踪的实体
        var currentEntry = dbContext.ChangeTracker.Entries<TEntity>().FirstOrDefault(x => x.Entity == entity);

        //currentEntry不为null，就将其State设置为EntityState.Detached，即取消跟踪该实体
        if (currentEntry != null)
        {
            //设置实体State为EntityState.Detached，取消跟踪该实体，之后dbContext.ChangeTracker.Entries().Count()的值会减1
            currentEntry.State = EntityState.Detached;
        }
    }

    /// <summary>
    /// 取消指定更新的实体
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="entities"></param>
    /// <typeparam name="T"></typeparam>
    public static void DetachSpecifyEntities<TEntity, TKey>(this DbContext dbContext, List<TEntity> entities)
        where TEntity : BaseEntity<TKey>
    {
        if (entities is not { Count: > 0 }) return;

        // 原实现逐个查找跟踪条目为 O(n×m)；改为按引用集合一次遍历。
        var targets = new HashSet<TEntity>(entities, ReferenceEqualityComparer.Instance);
        foreach (var currentEntry in dbContext.ChangeTracker.Entries<TEntity>().ToList())
        {
            if (targets.Contains(currentEntry.Entity))
            {
                currentEntry.State = EntityState.Detached;
            }
        }
    }
}