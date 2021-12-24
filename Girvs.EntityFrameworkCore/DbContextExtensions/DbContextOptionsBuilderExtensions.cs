using System;
using System.Data.Common;
using Girvs.EntityFrameworkCore.Configuration;
using Girvs.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace Girvs.EntityFrameworkCore.DbContextExtensions
{
    public static class DbContextOptionsBuilderExtensions
    {
        public static void UseSqlServerWithLazyLoading(this DbContextOptionsBuilder optionsBuilder,
            DataConnectionConfig config, string connStr)
        {
            optionsBuilder.UseSqlServer(connStr);
            optionsBuilder.UseBatchEF_MSSQL();
        }

        public static void UseMySqlWithLazyLoading(this DbContextOptionsBuilder optionsBuilder,
            DataConnectionConfig config, string connStr)
        {
            var serverVersion = new MySqlServerVersion(new Version(config.VersionNumber));

            optionsBuilder.UseMySql(connStr, serverVersion,
                builder => { builder.EnableRetryOnFailure(maxRetryCount: 5); });
            optionsBuilder.UseBatchEF_MySQLPomelo();
        }

        public static void UseSqlLiteWithLazyLoading(this DbContextOptionsBuilder optionsBuilder,
            DataConnectionConfig config, string connStr)
        {
            if (config.UseRowNumberForPaging)
            {
                optionsBuilder.UseSqlite(connStr,
                    option => option.CommandTimeout(config.SQLCommandTimeout));
                optionsBuilder.UseSqlite();
            }
            else
            {
                optionsBuilder.UseSqlServer(connStr,
                    option => option.CommandTimeout(config.SQLCommandTimeout));
                optionsBuilder.UseSqlServer();
            }
        }

        public static void UseOracleWithLazyLoading(this DbContextOptionsBuilder optionsBuilder,
            DataConnectionConfig config, string connStr)
        {
            optionsBuilder.UseOracle(connStr,
                option =>
                {
                    option.CommandTimeout(config.SQLCommandTimeout);
                    option.UseOracleSQLCompatibility(config.VersionNumber);
                });
            optionsBuilder.UseOracle();
        }

        public static void UseInMemoryWithLazyLoading(this DbContextOptionsBuilder optionsBuilder,
            DataConnectionConfig config, string connStr)
        {
            optionsBuilder.UseInMemoryDatabase(connStr);
        }

        public static void ConfigDbContextOptionsBuilderTransaction(this DbContextOptionsBuilder optionsBuilder,
            DbConnection connection, DataConnectionConfig config)
        {
            switch (config.UseDataType)
            {
                case UseDataType.MsSql:
                    optionsBuilder.UseSqlServer(connection);
                    break;

                case UseDataType.MySql:
                    optionsBuilder.UseMySql(connection, new MySqlServerVersion(config.VersionNumber));
                    break;
            }

            var loggerFactory = EngineContext.Current.Resolve<ILoggerFactory>();
            optionsBuilder.UseLoggerFactory(loggerFactory);
        }

        public static void ConfigDbContextOptionsBuilder(this DbContextOptionsBuilder optionsBuilder,
            DataConnectionConfig config, string connStr = null)
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

            // optionsBuilder.ReplaceService<IModelCacheKeyFactory, DynamicModelCacheKeyFactory>();
        }
    }
}