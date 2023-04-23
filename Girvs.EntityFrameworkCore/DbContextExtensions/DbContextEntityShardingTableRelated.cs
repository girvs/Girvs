namespace Girvs.EntityFrameworkCore.DbContextExtensions;

public class DbContextEntityShardingTableRelated
{
    private const string MigrationsHistoryTableName = "__EFMigrationsHistory{0}";

    public DbContextEntityShardingTableRelated(Type dbContextType)
    {
        DbContextType = dbContextType;
        EntityTypes = new List<EntityShardingTableParameter>();
    }

    public Type DbContextType { get; }

    /// <summary>
    /// 是否存在需要进行分表的实体
    /// </summary>
    public bool IsExistShardingTableEntity
    {
        get { return EntityTypes.Any(x => x.IsNeedShardingTable); }
    }

    public IList<EntityShardingTableParameter> EntityTypes { get; set; }

    /// <summary>
    /// 获取当前迁移文件的后缀
    /// </summary>
    /// <returns></returns>
    public string GetCurrentMigrationsHistoryShardingTableSuffix()
    {
        var shardingTableSuffix = string.Empty;
        if (EntityTypes.Any(x => x.IsAssignableTenantShardingTable))
        {
            shardingTableSuffix += EngineContext.Current.GetTenantShardingTableSuffix();
        }

        if (EntityTypes.Any(x => x.IsAssignableYearShardingTable))
        {
            shardingTableSuffix += EngineContext.Current.GetYearShardingTableSuffix();
        }

        return shardingTableSuffix;
    }
    
    /// <summary>
    /// 获取当前迁移文件
    /// </summary>
    /// <returns></returns>
    public string GetCurrentMigrationsHistoryShardingTableName()
    {
        return string.Format(MigrationsHistoryTableName, GetCurrentMigrationsHistoryShardingTableSuffix());
    }

    /// <summary>
    /// 是否存在指定的实体
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns></returns>
    public bool ExistEntity<TEntity>() where TEntity : Entity
    {
        return ExistEntity(typeof(TEntity));
    }

    /// <summary>
    /// 是否存在指定的实体
    /// </summary>
    /// <param name="entityType"></param>
    /// <returns></returns>
    public bool ExistEntity(Type entityType)
    {
        return EntityTypes.Any(x => x.EntityType == entityType);
    }

    /// <summary>
    /// 获取指定的实体
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns></returns>
    public EntityShardingTableParameter GetEntity<TEntity>() where TEntity : Entity
    {
        return GetEntity(typeof(TEntity));
    }

    /// <summary>
    /// 获取指定的实体
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns></returns>
    public EntityShardingTableParameter GetEntity(Type entityType)
    {
        return EntityTypes.First(x => x.EntityType == entityType);
    }

    /// <summary>
    /// 获取当前DbContext的相关实例
    /// </summary>
    /// <returns></returns>
    /// <exception cref="GirvsException"></exception>
    public GirvsDbContext GetInstant()
    {
        try
        {
            return EngineContext.Current.Resolve(DbContextType) as GirvsDbContext;
        }
        catch
        {
            throw new GirvsException("暂时不能获取相关的实例");
        }
    }
}