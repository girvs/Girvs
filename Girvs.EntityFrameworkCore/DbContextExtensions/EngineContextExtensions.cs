namespace Girvs.EntityFrameworkCore.DbContextExtensions;

public static class EngineContextExtensions
{
    private static readonly IList<DbContextEntityShardingTableRelated> DbContextEntityRelateds =
        new List<DbContextEntityShardingTableRelated>();

    static EngineContextExtensions()
    {
        FormatDbContextEntityRelated();
    }

    /// <summary>
    /// 初始化数据
    /// </summary>
    private static void FormatDbContextEntityRelated()
    {
        var typeFinder = new WebAppTypeFinder();
        var dbContextTypes = typeFinder.FindOfType<GirvsDbContext>();
        foreach (var dbContextType in dbContextTypes)
        {
            var related = new DbContextEntityShardingTableRelated(dbContextType);

            var dbSetProperties = dbContextType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var dbSetProperty in dbSetProperties)
            {
                var gas = dbSetProperty.PropertyType.GetGenericArguments();
                foreach (var type in gas)
                {
                    if (type.IsAssignableTo(typeof(Entity)))
                    {
                        related.EntityTypes.Add(new EntityShardingTableParameter(type));
                    }
                }
            }

            DbContextEntityRelateds.Add(related);
        }
    }

    public static DbContextEntityShardingTableRelated
        GetShardingTableRelatedByDbContext<TDbContext>(this IEngine engine) where TDbContext : GirvsDbContext
    {
        return GetShardingTableRelatedByDbContext(engine, typeof(TDbContext));
    }

    public static DbContextEntityShardingTableRelated GetShardingTableRelatedByDbContext(this IEngine engine,
        Type dbContextType)
    {
        return DbContextEntityRelateds.First(x => x.DbContextType == dbContextType);
    }

    /// <summary>
    /// 获取实体相关联的DbContext
    /// </summary>
    /// <param name="engine"></param>
    /// <returns></returns>
    /// <exception cref="GirvsException"></exception>
    public static DbContextEntityShardingTableRelated
        GetShardingTableRelatedByEntity<TEntity>(this IEngine engine) where TEntity : Entity
    {
        return GetShardingTableRelatedByEntity(engine, typeof(TEntity));
    }

    /// <summary>
    /// 获取实体相关联的DbContext
    /// </summary>
    /// <param name="engine"></param>
    /// <param name="entityType"></param>
    /// <returns></returns>
    /// <exception cref="GirvsException"></exception>
    public static DbContextEntityShardingTableRelated GetShardingTableRelatedByEntity(this IEngine engine,
        Type entityType)
    {
        return DbContextEntityRelateds.First(x => x.ExistEntity(entityType));
    }

    /// <summary>
    /// 获取实体分表相关的参数
    /// </summary>
    /// <param name="engine"></param>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns></returns>
    public static EntityShardingTableParameter GetEntityShardingTableParameter<TEntity>(this IEngine engine)
        where TEntity : Entity
    {
        return GetEntityShardingTableParameter(engine, typeof(TEntity));
    }

    /// <summary>
    /// 获取实体分表相关的参数
    /// </summary>
    /// <param name="engine"></param>
    /// <param name="entityType"></param>
    /// <returns></returns>
    public static EntityShardingTableParameter GetEntityShardingTableParameter(this IEngine engine, Type entityType)
    {
        var related = GetShardingTableRelatedByEntity(engine, entityType);
        return related.GetEntity(entityType);
    }

    /// <summary>
    /// 获取实现ITenantShardingTable接口，分表的后缀名
    /// </summary>
    /// <param name="engine"></param>
    /// <returns></returns>
    internal static string GetTenantShardingTableSuffix(this IEngine engine)
    {
        try
        {
            var tenantId = engine.ClaimManager.IdentityClaim.GetTenantId<Guid>();
            if (tenantId != Guid.Empty)
            {
                return $"_{tenantId.ToString().Replace("-", "")}";
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 获取实现IYearShardingTable接口，分表的后缀名
    /// </summary>
    /// <param name="engine"></param>
    /// <returns></returns>
    internal static string GetYearShardingTableSuffix(this IEngine engine)
    {
        return $"_{DateTime.Now.Year}";
    }
}