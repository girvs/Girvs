using System.Collections.Concurrent;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore.Storage;

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

        if (string.Equals(dbContext.Database.GetConnectionString(), connStr, StringComparison.Ordinal))
            return;

        if (dbContext.Database.CurrentTransaction != null || System.Transactions.Transaction.Current != null)
            throw new GirvsException("事务中不允许切换数据库，请在开启事务前选择主库");

        if (conn.State != ConnectionState.Closed)
            throw new GirvsException("连接打开时不允许切换数据库，请先关闭连接并在主库开启写操作");

        dbContext.Database.SetConnectionString(connStr);

        // 切换成功后记录实际目标数据库，不记录可能包含密码的连接串。
        logger?.LogInformation(
            "当前 DbContextId 为 {DbContextId}，当前数据的状态为 {ConnectionState}，切换数据库模式为 {DataBaseWriteAndRead}，目标数据库为 {DatabaseName}",
            dbContext.ContextId.InstanceId,
            conn.State,
            dataBaseWriteAndRead,
            conn.Database
        );
    }

    /// <summary>
    /// 先选择主库，再开启显式写事务。调用方负责提交、回滚及释放；同一作用域仓储使用同一上下文参与事务。
    /// </summary>
    public static Task<IDbContextTransaction> BeginWriteTransactionAsync(
        this DbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        dbContext.SwitchReadWriteDataBase(DataBaseWriteAndRead.Write);
        return dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    private sealed record MigrationCacheKey(Type ContextType, string Provider, string ConnectionFingerprint, string Suffix);

    private sealed class MigrationState
    {
        public bool Applied { get; set; }
    }

    // 仅协调当前进程；跨实例须使用部署锁或确认提供程序支持迁移锁。
    private static readonly ConcurrentDictionary<MigrationCacheKey, MigrationState> Migrations = new();

    /// <summary>
    /// 在分表的情况下进行自动迁移
    /// </summary>
    /// <param name="dbContext"></param>
    public static void ShardingAutoMigration(this DbContext dbContext)
    {
        
        var dbContextEntityShardingTableRelated =
            EngineContext.Current.GetShardingTableRelatedByDbContext(dbContext.GetType());
        var config = EngineContext.Current.GetAppModuleConfig<DbConfig>().GetDataConnectionConfig(dbContext.GetType());
        if (!config.EnableShardingTable || !dbContextEntityShardingTableRelated.IsExistShardingTableEntity)
            return;

        var suffix = dbContextEntityShardingTableRelated.GetCurrentMigrationsHistoryShardingTableSuffix();
        
        if (suffix.IsNullOrEmpty())
            return;

        // 缓存只保存连接指纹，不保存连接串中的凭据；主库标识不受本次随机读库选择影响。
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(config.GetMasterDataConnectionString())));
        var key = new MigrationCacheKey(dbContext.GetType(), dbContext.Database.ProviderName, fingerprint, suffix);
        var state = Migrations.GetOrAdd(key, static _ => new MigrationState());
        lock (state)
        {
            if (state.Applied) return;
            dbContext.SwitchReadWriteDataBase(DataBaseWriteAndRead.Write);
            dbContext.Database.Migrate();
            // 失败时不写入完成状态，后续调用可重试。
            state.Applied = true;
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
