using Zack.EFCore.Batch.Oracle;

namespace Girvs.EntityFrameworkCore.DbContextExtensions;

public static class DbContextOptionsBuilderExtensions
{
    public static void UseSqlServerWithLazyLoading(this DbContextOptionsBuilder optionsBuilder,
        DataConnectionConfig config, string connStr)
    {
        optionsBuilder.UseSqlServer(connStr,
            builder =>
            {
                if (config.IsTenantShardingTable)
                {
                    builder.MigrationsHistoryTable(
                        $"__EFMigrationsHistory{EngineContext.Current.GetSafeShardingTableSuffix()}");
                }
            });
        
        optionsBuilder.UseBatchEF_MSSQL();
    }

    public static void UseMySqlWithLazyLoading(this DbContextOptionsBuilder optionsBuilder,
        DataConnectionConfig config, string connStr)
    {
        var serverVersion = new MySqlServerVersion(new Version(config.VersionNumber));

        optionsBuilder.UseMySql(connStr, serverVersion,
            builder =>
            {
                builder.EnableRetryOnFailure(maxRetryCount: 5);
                if (config.IsTenantShardingTable)
                {
                    builder.MigrationsHistoryTable(
                        $"__EFMigrationsHistory{EngineContext.Current.GetSafeShardingTableSuffix()}");
                }
            });
        optionsBuilder.UseBatchEF_MySQLPomelo();
    }

    public static void UseSqlLiteWithLazyLoading(this DbContextOptionsBuilder optionsBuilder,
        DataConnectionConfig config, string connStr)
    {
        if (config.UseRowNumberForPaging)
        {
            optionsBuilder.UseSqlite(connStr,
                builder =>
                {
                    builder.CommandTimeout(config.SQLCommandTimeout);
                    if (config.IsTenantShardingTable)
                    {
                        builder.MigrationsHistoryTable(
                            $"__EFMigrationsHistory{EngineContext.Current.GetSafeShardingTableSuffix()}");
                    }
                });

            optionsBuilder.UseBatchEF_Sqlite();
        }
        else
        {
            optionsBuilder.UseSqlServer(connStr,
                builder =>
                {
                    builder.CommandTimeout(config.SQLCommandTimeout);
                    if (config.IsTenantShardingTable)
                    {
                        builder.MigrationsHistoryTable(
                            $"__EFMigrationsHistory{EngineContext.Current.GetSafeShardingTableSuffix()}");
                    }
                });

            optionsBuilder.UseBatchEF_MSSQL();
        }
    }

    public static void UseOracleWithLazyLoading(this DbContextOptionsBuilder optionsBuilder,
        DataConnectionConfig config, string connStr)
    {
        optionsBuilder.UseOracle(connStr,
            builder =>
            {
                builder.CommandTimeout(config.SQLCommandTimeout);
                builder.UseOracleSQLCompatibility(config.VersionNumber);
                if (config.IsTenantShardingTable)
                {
                    builder.MigrationsHistoryTable(
                        $"__EFMigrationsHistory{EngineContext.Current.GetSafeShardingTableSuffix()}");
                }
            });
        optionsBuilder.UseBatchEF_Oracle();
    }

    public static void UseInMemoryWithLazyLoading(this DbContextOptionsBuilder optionsBuilder,
        DataConnectionConfig config, string connStr)
    {
        optionsBuilder.UseInMemoryDatabase(connStr, builder =>
        {
            // builder.MigrationsHistoryTable(
            //     $"__EFMigrationsHistory_{EngineContext.Current.GetSafeShardingTableSuffix()}");
        });
    }

    public static void ConfigDbContextOptionsBuilder<TDbContext>(this DbContextOptionsBuilder optionsBuilder,
        DataConnectionConfig config, string connStr = null) where TDbContext : GirvsDbContext
    {
        var dataConnectionConfig = config;
        connStr ??= config.MasterDataConnectionString;

        switch (dataConnectionConfig.UseDataType)
        {
            case UseDataType.MsSql:
                optionsBuilder.UseSqlServerWithLazyLoading(dataConnectionConfig, connStr);
                break;

            case UseDataType.MySql:
                optionsBuilder.UseMySqlWithLazyLoading(dataConnectionConfig, connStr);
                break;

            case UseDataType.SqlLite:
                optionsBuilder.UseSqlLiteWithLazyLoading(dataConnectionConfig, connStr);
                break;

            case UseDataType.Oracle:
                optionsBuilder.UseOracleWithLazyLoading(dataConnectionConfig, connStr);
                break;
        }

        if (dataConnectionConfig.UseLazyLoading)
        {
            optionsBuilder.UseLazyLoadingProxies();
        }

        if (!dataConnectionConfig.UseDataTracking)
        {
            optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        }

        if (dataConnectionConfig.EnableSensitiveDataLogging)
        {
            var loggerFactory = EngineContext.Current.Resolve<ILoggerFactory>();
            optionsBuilder.UseLoggerFactory(loggerFactory).EnableSensitiveDataLogging();
        }

        if (dataConnectionConfig.IsTenantShardingTable)
        {
            optionsBuilder.ReplaceService<IModelCacheKeyFactory, GirvsTenantModelCacheKeyFactory<TDbContext>>();
            optionsBuilder.ReplaceService<IMigrationsAssembly, GirvsMigrationByTenantAssembly>();
        }
    }
}