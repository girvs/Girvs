namespace Girvs.EntityFrameworkCore.DbContextExtensions;

public static class EngineContextExtensions
{
    private static readonly IDictionary<Type, Type> RelatedDbContextCache = new Dictionary<Type, Type>();
    private static object _async = new object();

    /// <summary>
    /// 获取实体相关联的DbContext
    /// </summary>
    /// <param name="engine"></param>
    /// <param name="entityType"></param>
    /// <returns></returns>
    /// <exception cref="GirvsException"></exception>
    public static DbContext GetEntityRelatedDbContext<TEntity>(this IEngine engine) where TEntity : Entity
    {
        return GetEntityRelatedDbContext(engine, typeof(TEntity));
    }

    /// <summary>
    /// 获取实体相关联的DbContext
    /// </summary>
    /// <param name="engine"></param>
    /// <param name="entityType"></param>
    /// <returns></returns>
    /// <exception cref="GirvsException"></exception>
    public static DbContext GetEntityRelatedDbContext(this IEngine engine, Type entityType)
    {
        lock (_async)
        {
            if (!RelatedDbContextCache.ContainsKey(entityType))
            {
                var typeFinder = EngineContext.Current.Resolve<ITypeFinder>();
                var ts = typeFinder?.FindOfType(typeof(GirvsDbContext));

                Type relatedDbContextType = null;
                if (ts != null)
                    foreach (var dbContextType in ts)
                    {
                        if (EngineContext.Current.Resolve(dbContextType) is GirvsDbContext dbContext)
                        {
                            var modelTypes = dbContext.Model.GetEntityTypes().Select(x => x.ClrType);
                            if (modelTypes.Any(x => x.Name == entityType.Name))
                            {
                                relatedDbContextType = dbContextType;
                                break;
                            }
                        }
                    }

                if (relatedDbContextType == null)
                {
                    throw new GirvsException($"未找到对应的DbContext   {entityType.Name}");
                }

                RelatedDbContextCache.Add(entityType, relatedDbContextType);
            }

            return EngineContext.Current.Resolve(RelatedDbContextCache[entityType]) as DbContext;
        }
    }

    /// <summary>
    /// 获取Migrations时   Migration的表名
    /// </summary>
    /// <param name="engine"></param>
    /// <param name="dbContext"></param>
    /// <returns></returns>
    public static string GetMigrationsShardingTableSuffix(this IEngine engine, DbContext dbContext)
    {
        try
        {
            var shardingTableSuffix = string.Empty;
            var entityTypes = dbContext.Model.GetEntityTypes().Select(x=>x.ClrType);

            //是否存在实体包含
            var existTenantShardingTable = entityTypes.Any(x => x.IsAssignableTo(typeof(ITenantShardingTable)));

            //当前如果包含租户分表标识

            if (existTenantShardingTable)
            {
                var tenantId = engine.ClaimManager.IdentityClaim.GetTenantId<Guid>();
                if (tenantId != Guid.Empty)
                {
                    shardingTableSuffix = $"_{tenantId.ToString().Replace("-", "")}";
                }
            }

            //当前如果包含按年份分表标识
            var existYearShardingTable = entityTypes.Any(x => x.IsAssignableTo(typeof(IYearShardingTable)));
            if (existYearShardingTable)
            {
                shardingTableSuffix = $"{shardingTableSuffix}_{DateTime.Now.Year}";
            }

            return shardingTableSuffix;
        }
        catch (Exception e)
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 获取实体表名对应的后缀名称
    /// </summary>
    /// <param name="engine"></param>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns></returns>
    public static string GetSafeShardingTableSuffix<TEntity>(this IEngine engine) where TEntity : Entity
    {
        return GetSafeShardingTableSuffix(engine, typeof(TEntity));
    }

    /// <summary>
    /// 获取实体表名对应的后缀名称
    /// </summary>
    /// <param name="engine"></param>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns></returns>
    public static string GetSafeShardingTableSuffix(this IEngine engine, Type entityType)
    {
        try
        {
            var shardingTableSuffix = string.Empty;
            //判断当前实体是否需要进行分表
            var currentEntityIsNeedShardingTable = IsNeedShardingTable(engine, entityType);
            if (!currentEntityIsNeedShardingTable)
                return shardingTableSuffix;

            //当前如果包含租户分表标识
            var isIncludeTenantShardingTable = entityType.IsAssignableTo(typeof(ITenantShardingTable));
            if (isIncludeTenantShardingTable)
            {
                var tenantId = engine.ClaimManager.IdentityClaim.GetTenantId<Guid>();
                if (tenantId != Guid.Empty)
                {
                    shardingTableSuffix = $"_{tenantId.ToString().Replace("-", "")}";
                }
            }

            //当前如果包含按年份分表标识
            var isIncludeYearShardingTable = entityType.IsAssignableTo(typeof(IYearShardingTable));
            if (isIncludeYearShardingTable)
            {
                shardingTableSuffix = $"{shardingTableSuffix}_{DateTime.Now.Year}";
            }

            return shardingTableSuffix;
        }
        catch (Exception e)
        {
            return string.Empty;
        }
    }


    /// <summary>
    /// 判断当前实体是否需要进行分表
    /// </summary>
    /// <param name="engine"></param>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns></returns>
    public static bool IsNeedShardingTable<TEntity>(this IEngine engine) where TEntity : Entity
    {
        return IsNeedShardingTable(engine, typeof(TEntity));
    }

    /// <summary>
    /// 判断当前实体是否需要进行分表
    /// </summary>
    /// <param name="engine"></param>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns></returns>
    public static bool IsNeedShardingTable(this IEngine engine, Type entityType)
    {
        try
        {
            var currentTenant = engine.ClaimManager.IdentityClaim.GetTenantId<Guid>();
            if (currentTenant == Guid.Empty)
                return false;

            //当前实体是否包含分表的标识
            var isIncludeShardTag = entityType.IsAssignableTo(typeof(ITenantShardingTable)) ||
                                    entityType.IsAssignableTo(typeof(IYearShardingTable));

            //当前配置文件是否打开了分表的标识
            var dbContext = engine.GetEntityRelatedDbContext(entityType);
            var dataProviderConfig =
                engine.GetAppModuleConfig<DbConfig>().GetDataConnectionConfig(dbContext.GetType());

            return dataProviderConfig.IsTenantShardingTable && isIncludeShardTag;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取指定实体的表名
    /// </summary>
    /// <param name="engine"></param>
    /// <param name="entityType"></param>
    /// <returns></returns>
    public static string GetMigrationEntityTableName<TEntity>(this IEngine engine) where TEntity : Entity
    {
        return GetMigrationEntityTableName(engine, typeof(TEntity));
    }

    /// <summary>
    /// 获取指定实体的表名
    /// </summary>
    /// <param name="engine"></param>
    /// <param name="entityType"></param>
    /// <returns></returns>
    public static string GetMigrationEntityTableName(this IEngine engine, Type entityType)
    {
        if (!IsNeedShardingTable(engine, entityType)) return entityType.Name;
        var suffix = GetSafeShardingTableSuffix(engine, entityType);
        return $"{entityType.Name}{suffix}";
    }
}