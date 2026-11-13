using System.Data.Common;
using Girvs.EntityFrameworkCore.TableManager;
using Humanizer;

namespace Girvs.EntityFrameworkCore.DbContextExtensions;

public static class EngineContextExtensions
{
    // 仓储每次实例化都会按实体查找所属上下文，预建索引避免线性扫描。
    private static readonly Dictionary<Type, DbContextEntityShardingTableRelated> DbContextRelatedMap =
        new();

    private static readonly Dictionary<Type, DbContextEntityShardingTableRelated> EntityRelatedMap =
        new();

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

            DbContextRelatedMap.TryAdd(dbContextType, related);

            foreach (var entityType in related.EntityTypes.Select(x => x.EntityType))
            {
                // 与原 First(...) 语义一致：同一实体出现在多个上下文时保留最先发现的上下文。
                EntityRelatedMap.TryAdd(entityType, related);
            }
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
        if (DbContextRelatedMap.TryGetValue(dbContextType, out var related))
        {
            return related;
        }

        // 保持原 First(...) 的异常类型与语义。
        throw new InvalidOperationException("Sequence contains no matching element");
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
        if (string.IsNullOrWhiteSpace(connectionString)) return string.Empty;

        try
        {
            // 用连接串构建器解析，大小写不敏感且兼容转义值；原正则只覆盖 database/DataBase 两种写法。
            var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
            return builder.TryGetValue("Database", out var database)
                ? database?.ToString() ?? string.Empty
                : string.Empty;
        }
        catch (ArgumentException)
        {
            return string.Empty;
        }
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
            var tenantId = engine.PrincipalAccessor.Principal.GetTenantId<Guid>();
            if (tenantId != Guid.Empty)
            {
                var tenantIdStr = $"_{tenantId:N}";
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
        if (EntityRelatedMap.TryGetValue(entityType, out var related))
        {
            return related;
        }

        // 保持原 First(...) 的异常类型与语义。
        throw new InvalidOperationException("Sequence contains no matching element");
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
            var tenantId = engine.PrincipalAccessor.Principal.GetTenantId<Guid>();
            if (tenantId != Guid.Empty)
            {
                return $"_{tenantId:N}";
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
