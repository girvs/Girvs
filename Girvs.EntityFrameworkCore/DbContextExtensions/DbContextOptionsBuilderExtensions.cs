namespace Girvs.EntityFrameworkCore.DbContextExtensions;

public static class DbContextOptionsBuilderExtensions
{
    public static void UseSqlServerWithLazyLoading<TDbContext>(
        this DbContextOptionsBuilder optionsBuilder,
        DataConnectionConfig config,
        string connStr
    )
        where TDbContext : GirvsDbContext
    {
        optionsBuilder.UseSqlServer(
            connStr,
            builder =>
            {
                if (config.EnableShardingTable)
                {
                    var related =
                        EngineContext.Current.GetShardingTableRelatedByDbContext<TDbContext>();
                    builder.MigrationsHistoryTable(
                        related.GetCurrentMigrationsHistoryShardingTableName()
                    );
                }
            }
        );

        // optionsBuilder.UseBatchEF_MSSQL();
    }

    public static void UseMySqlWithLazyLoading<TDbContext>(
        this DbContextOptionsBuilder optionsBuilder,
        DataConnectionConfig config,
        string connStr
    )
        where TDbContext : GirvsDbContext
    {
        var serverVersion = new MySqlServerVersion(new Version(config.VersionNumber));

        optionsBuilder.UseMySql(
            connStr,
            serverVersion,
            builder =>
            {
                builder.EnableRetryOnFailure(maxRetryCount: 5);
                if (config.EnableShardingTable)
                {
                    var related =
                        EngineContext.Current.GetShardingTableRelatedByDbContext<TDbContext>();
                    builder.MigrationsHistoryTable(
                        related.GetCurrentMigrationsHistoryShardingTableName()
                    );
                }
            }
        );
    }

    public static void ConfigDbContextOptionsBuilder<TDbContext>(
        this DbContextOptionsBuilder optionsBuilder,
        DataConnectionConfig config,
        string connStr = null
    )
        where TDbContext : GirvsDbContext
    {
        var dataConnectionConfig = config;
        connStr ??= config.MasterDataConnectionString;

        switch (dataConnectionConfig.UseDataType)
        {
            case UseDataType.MsSql:
                optionsBuilder.UseSqlServerWithLazyLoading<TDbContext>(
                    dataConnectionConfig,
                    connStr
                );
                break;

            case UseDataType.MySql:
                optionsBuilder.UseMySqlWithLazyLoading<TDbContext>(dataConnectionConfig, connStr);
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

        if (dataConnectionConfig.EnableShardingTable)
        {
            optionsBuilder.ReplaceService<
                IModelCacheKeyFactory,
                GirvsTenantModelCacheKeyFactory<TDbContext>
            >();
            optionsBuilder.ReplaceService<IMigrationsAssembly, GirvsMigrationByTenantAssembly>();
        }
    }
}
