namespace Girvs.EntityFrameworkCore.DbContextExtensions;

public static class DbContextExtension
{
    /// <summary>
    /// 切换读写数据库
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="dataBaseWriteAndRead"></param>
    public static void SwitchReadWriteDataBase(this DbContext dbContext, DataBaseWriteAndRead dataBaseWriteAndRead)
    {
        var currentDbContextConfig = EngineContext.Current.GetAppModuleConfig<DbConfig>()
            ?.GetDataConnectionConfig(dbContext.GetType());

        var logger = EngineContext.Current.Resolve<ILogger<object>>();
        var connStr = dataBaseWriteAndRead == DataBaseWriteAndRead.Write
            ? currentDbContextConfig.GetMasterDataConnectionString()
            : currentDbContextConfig.GetSecureRandomReadDataConnectionString();

        var conn = dbContext.Database.GetDbConnection();

        // 不记录连接串原文（可能包含密码），仅记录目标数据库与连接状态。
        logger?.LogInformation(
            "当前 DbContextId 为 {DbContextId}，当前数据的状态为 {ConnectionState}，切换数据库模式为 {DataBaseWriteAndRead}，目标数据库为 {DatabaseName}",
            dbContext.ContextId.InstanceId,
            conn.State,
            dataBaseWriteAndRead,
            conn.Database
        );

        conn.ConnectionString = connStr;
    }


    // 分表后缀需要去重且高频查询，用 HashSet 保持 O(1)。
    private static readonly HashSet<string> MigrationSuffixs = new();
    private static readonly object MigrationLock = new();

    /// <summary>
    /// 在分表的情况下进行自动迁移
    /// </summary>
    /// <param name="dbContext"></param>
    public static void ShardingAutoMigration(this DbContext dbContext)
    {
        
        var dbContextEntityShardingTableRelated =
            EngineContext.Current.GetShardingTableRelatedByDbContext(dbContext.GetType());
        var suffix = dbContextEntityShardingTableRelated.GetCurrentMigrationsHistoryShardingTableSuffix();
        
        if (suffix.IsNullOrEmpty())
            return;

        lock (MigrationLock)
        {
            if (MigrationSuffixs.Contains(suffix)) return;
            if (GetNeedMigrationEntities().Count > 0)
            {
                //切换到写的数据库
                dbContext.SwitchReadWriteDataBase(DataBaseWriteAndRead.Write);
                dbContext.Database.Migrate();
            }

            MigrationSuffixs.Add(suffix);
        }
    }

    private static List<Type> GetNeedMigrationEntities()
    {
        try
        {
            var typeFinder = EngineContext.Current.Resolve<ITypeFinder>();
            var result = typeFinder.FindOfType<ITenantShardingTable>().ToList();
            return result;
        }
        catch
        {
            return new List<Type>();
        }
    }


    // /// <summary>
    // /// 移除不需分表的实体
    // /// </summary>
    // /// <param name="dbContext"></param>
    // public static void RemoveDbContextRelationModelNotShardTableEntity(this DbContext dbContext)
    // {
    //     var contextModel = dbContext.Model as Model;
    //     var contextModelRelationalModel = contextModel.RelationalModel as RelationalModel;
    //
    //     IList<(string, string)> needRemoveTableKey = new List<(string, string)>();
    //
    //     foreach (var table in contextModelRelationalModel.Tables)
    //     {
    //         var isRemove = table.Value.EntityTypeMappings.Any(x =>
    //         {
    //             var entityType = x.EntityType.ClrType;
    //             return entityType.IsAssignableTo(typeof(ITenantShardingTable));
    //         });
    //
    //         if (!isRemove)
    //         {
    //             needRemoveTableKey.Add(table.Key);
    //         }
    //     }
    //
    //     if (needRemoveTableKey.Count > 0)
    //     {
    //         foreach (var valueTuple in needRemoveTableKey)
    //         {
    //             contextModelRelationalModel.Tables.Remove(valueTuple);
    //         }
    //     }
    // }
    //
    //
    // /// <summary>
    // /// 移除所有的除了我指定的那个类型
    // /// </summary>
    // /// <param name="dbContext"></param>
    // /// <param name="shardingType"></param>
    // public static void RemoveDbContextRelationModelTableEntityWithType(this DbContext dbContext,
    //     Type shardingType)
    // {
    //     var contextModel = dbContext.Model as Model;
    //     var contextModelRelationalModel = contextModel.RelationalModel as RelationalModel;
    //     var valueTuples = contextModelRelationalModel.Tables
    //         .Where(o => o.Value.EntityTypeMappings.All(m => m.EntityType.ClrType != shardingType))
    //         .Select(o => o.Key).ToList();
    //     for (int i = 0; i < valueTuples.Count; i++)
    //     {
    //         contextModelRelationalModel.Tables.Remove(valueTuples[i]);
    //     }
    // }
    //
    // /// <summary>
    // /// 移除所有的除了我指定的那个类型
    // /// </summary>
    // public static void RemoveDbContextRelationModelTableEntityWithType<TEntity>(this DbContext dbContext)
    //     where TEntity : Entity
    // {
    //     RemoveDbContextRelationModelTableEntityWithType(dbContext, typeof(TEntity));
    // }
    //
    // /// <summary>
    // /// 创建指定的表
    // /// </summary>
    // /// <param name="shardingEntityType"></param>
    // public static void CreateTable<TEntity>(this DbContext dbContext) where TEntity : Entity
    // {
    //     CreateTable(dbContext, typeof(TEntity));
    // }
    //
    // /// <summary>
    // /// 创建指定的表
    // /// </summary>
    // /// <param name="shardingEntityType"></param>
    // public static void CreateTable(this DbContext dbContext, Type shardingEntityType)
    // {
    //     // 需要判断表是否已经存在
    //     dbContext.RemoveDbContextRelationModelTableEntityWithType(shardingEntityType);
    //     CreateTables(dbContext);
    // }
    //
    // /// <summary>
    // /// 创建租户表
    // /// </summary>
    // /// <param name="dbContext"></param>
    // /// <exception cref="GirvsException"></exception>
    // public static void CreateTenantShardTable(this DbContext dbContext)
    // {
    //     // 需要判断表是否已经存在
    //     dbContext.RemoveDbContextRelationModelNotShardTableEntity();
    //     CreateTables(dbContext);
    // }
    //
    // private static void CreateTables(DbContext dbContext)
    // {
    //     var databaseCreator = dbContext.Database.GetService<IDatabaseCreator>() as RelationalDatabaseCreator;
    //     try
    //     {
    //         databaseCreator.CreateTables();
    //     }
    //     catch (Exception ex)
    //     {
    //         // throw new GirvsException($" create table error :{ex.Message}", ex);
    //     }
    // }
}
