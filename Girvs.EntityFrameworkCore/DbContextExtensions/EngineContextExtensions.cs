namespace Girvs.EntityFrameworkCore.DbContextExtensions;

public static class EngineContextExtensions
{
    private static readonly IDictionary<Type, Type> _relatedDbContextCache = new Dictionary<Type, Type>();
    private static object _async = new object();

    public static DbContext GetEntityRelatedDbContext<TEntity>(this IEngine engine) where TEntity : Entity
    {
        lock (_async)
        {
            var entityType = typeof(TEntity);
            if (!_relatedDbContextCache.ContainsKey(entityType))
            {
                var typeFinder = EngineContext.Current.Resolve<ITypeFinder>();
                var ts = typeFinder?.FindOfType(typeof(GirvsDbContext));

                var dbContextType = ts?
                    .FirstOrDefault(x =>
                        x.GetProperties().Any(propertyInfo => propertyInfo.PropertyType == typeof(DbSet<TEntity>)));

                if (dbContextType == null)
                {
                    throw new GirvsException($"未找到对应的DbContext   {typeof(TEntity).Name}");
                }

                _relatedDbContextCache.Add(entityType, dbContextType);
            }

            return EngineContext.Current.Resolve(_relatedDbContextCache[entityType]) as DbContext;
        }
    }

    public static string GetSafeShardingTableSuffix(this IEngine engine)
    {
        try
        {
            var shardingTag = engine.ClaimManager.IdentityClaim.TenantId;
            if (Guid.Parse(shardingTag) == Guid.Empty)
            {
                return string.Empty;
            }

            return shardingTag.IsNullOrEmpty() ? string.Empty : $"_{shardingTag.Replace("-", "")}";
        }
        catch (Exception e)
        {
            return string.Empty;
        }
    }

    public static bool IsNeedShardingTable<TEntity>(this IEngine engine) where TEntity : Entity
    {
        try
        {
            var suffix = GetSafeShardingTableSuffix(engine);
            if (suffix.IsNullOrEmpty()) //默认为超级管理员，则直接进行还原
            {
                return true;
            }
            var entityType = typeof(TEntity);
            var dbContext = engine.GetEntityRelatedDbContext<TEntity>();
            var dataProviderConfig =
                engine.GetAppModuleConfig<DbConfig>().GetDataConnectionConfig(dbContext.GetType());

            return dataProviderConfig.IsTenantShardingTable && !suffix.IsNullOrEmpty() &&
                   entityType.IsAssignableTo(typeof(ITenantShardingTable));
        }
        catch
        {
            return false;
        }
    }

    public static string GetMigrationEntityTableName<TEntity>(this IEngine engine) where TEntity : Entity
    {
        var entityType = typeof(TEntity);
        if (!IsNeedShardingTable<TEntity>(engine)) return entityType.Name;
        var suffix = GetSafeShardingTableSuffix(engine);
        return $"{entityType.Name}{suffix}";
    }
}