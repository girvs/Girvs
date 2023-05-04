using Girvs.EntityFrameworkCore.TableManager;
using Humanizer;

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
    /// 获取当前实体对应的数据Schema名称
    /// </summary>
    /// <param name="engine"></param>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns></returns>
    public static string GetDbContextSchemaName<TEntity>(this IEngine engine) where TEntity : Entity
    {
        var related = GetShardingTableRelatedByEntity<TEntity>(engine);
        var dbContext = engine.Resolve(related.DbContextType) as DbContext;
        var connectionString = dbContext.Database.GetConnectionString();
        var beginStr1 = "database=";
        var beginStr2 = "DataBase=";
        var endStr = ";";
        string result;
        try
        {
            result = System.Text.RegularExpressions.Regex.Match(connectionString, $"{beginStr1}(.*?){endStr}")
                .Result("$1");
        }
        catch
        {
            result = System.Text.RegularExpressions.Regex.Match(connectionString, $"{beginStr2}(.*?){endStr}")
                .Result("$1");
        }

        return result;
    }

    /// <summary>
    /// 获取当前实体所有
    /// </summary>
    /// <param name="engine"></param>
    /// <param name="isIncludeCurrentTenantId"></param>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns></returns>
    /// <exception cref="GirvsException"></exception>
    public static async Task<List<string>> GetShardingTableNamesByEntity<TEntity>(this IEngine engine,
        bool isIncludeCurrentTenantId = false) where TEntity : Entity
    {
        var tableManager = engine.Resolve<ITableManager>();
        if (tableManager == null)
        {
            throw new GirvsException("ITableManager 接口未实现，需要此功能需要在Infrastructure中实现此接口");
        }

        var related = GetShardingTableRelatedByEntity<TEntity>(engine);
        var dbContext = engine.Resolve(related.DbContextType) as DbContext;
        var schema = GetDbContextSchemaName<TEntity>(engine);
        var entityTableName = typeof(TEntity).Name;
        var tableNames = await tableManager.GetEntityAllTableNames(dbContext, schema, entityTableName);

        if (isIncludeCurrentTenantId)
        {
            var tenantId = engine.ClaimManager.IdentityClaim.GetTenantId<Guid>();
            if (tenantId != Guid.Empty)
            {
                var tenantIdStr = $"_{tenantId.ToString().Replace("-", "")}";
                return tableNames.Where(x => x.Contains(tenantIdStr)).ToList();
            }
        }

        return tableNames;
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