using System;
using System.Linq;
using Girvs.Domain.Configuration;
using Girvs.Domain.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace Girvs.Infrastructure.DbContextExtensions
{
    public static class DbContextOptionsBuilderExtensions
    {
        private static DbContextOptionsBuilder UseLazyLoading(DbContextOptionsBuilder optionsBuilder,
            DataConnectionConfig config)
        {
            if (config.UseLazyLoading)
            {
                return optionsBuilder.UseLazyLoadingProxies();
            }

            return optionsBuilder;
        }

        public static string GetConnectionString(DataBaseWriteAndRead dataBaseWriteAndRead,
            DataConnectionConfig dataConnectionConfig)
        {
            if (dataBaseWriteAndRead == DataBaseWriteAndRead.Write)
                return dataConnectionConfig.MasterDataConnectionString;

            if (!dataConnectionConfig.ReadDataConnectionString.Any())
                return dataConnectionConfig.MasterDataConnectionString;

            if (dataConnectionConfig.ReadDataConnectionString.Count == 1)
            {
                return dataConnectionConfig.ReadDataConnectionString[0];
            }
            else
            {
                Random r = new Random();
                var index = r.Next(0, dataConnectionConfig.ReadDataConnectionString.Count);
                return dataConnectionConfig.ReadDataConnectionString[index];
            }
        }

        public static void UseSqlServerWithLazyLoading(this DbContextOptionsBuilder optionsBuilder
            , DataConnectionConfig config, DataBaseWriteAndRead dataBaseWriteAndRead = DataBaseWriteAndRead.Write)
        {
            var connStr = GetConnectionString(dataBaseWriteAndRead, config);
            var dbContextOptionsBuilder = UseLazyLoading(optionsBuilder, config);
            dbContextOptionsBuilder.UseSqlServer(connStr);
        }

        public static void UseMySqlWithLazyLoading(this DbContextOptionsBuilder optionsBuilder
            , DataConnectionConfig config, DataBaseWriteAndRead dataBaseWriteAndRead = DataBaseWriteAndRead.Write)
        {
            var dbContextOptionsBuilder = UseLazyLoading(optionsBuilder, config);
            var connStr = GetConnectionString(dataBaseWriteAndRead, config);

            if (config.UseRowNumberForPaging)
                dbContextOptionsBuilder.UseMySql(connStr,
                    option => { option.CommandTimeout(config.SQLCommandTimeout); });
            //dbContextOptionsBuilder.UseMySQL(spConfig.DataConnectionString, option => option.CommandTimeout(spConfig.SQLCommandTimeout));
            else
                dbContextOptionsBuilder.UseSqlServer(connStr,
                    option => option.CommandTimeout(config.SQLCommandTimeout));
        }

        public static void UseSqlLiteWithLazyLoading(this DbContextOptionsBuilder optionsBuilder
            , DataConnectionConfig config, DataBaseWriteAndRead dataBaseWriteAndRead = DataBaseWriteAndRead.Write)
        {
            var dbContextOptionsBuilder = UseLazyLoading(optionsBuilder, config);
            var connStr = GetConnectionString(dataBaseWriteAndRead, config);

            if (config.UseRowNumberForPaging)
                dbContextOptionsBuilder.UseSqlite(connStr,
                    option => option.CommandTimeout(config.SQLCommandTimeout));
            else
                dbContextOptionsBuilder.UseSqlServer(connStr,
                    option => option.CommandTimeout(config.SQLCommandTimeout));
        }

        public static void UseOracleWithLazyLoading(this DbContextOptionsBuilder optionsBuilder
            , DataConnectionConfig config, DataBaseWriteAndRead dataBaseWriteAndRead = DataBaseWriteAndRead.Write)
        {
            var dbContextOptionsBuilder = UseLazyLoading(optionsBuilder, config);
            var connStr = GetConnectionString(dataBaseWriteAndRead, config);

            dbContextOptionsBuilder.UseOracle(connStr,
                option =>
                {
                    option.CommandTimeout(config.SQLCommandTimeout);
                    option.UseOracleSQLCompatibility(config.VersionNumber);
                });
        }

        public static void UseInMemoryWithLazyLoading(this DbContextOptionsBuilder optionsBuilder
            , DataConnectionConfig config, DataBaseWriteAndRead dataBaseWriteAndRead = DataBaseWriteAndRead.Write)
        {
            var connStr = GetConnectionString(dataBaseWriteAndRead, config);

            var dbContextOptionsBuilder = UseLazyLoading(optionsBuilder, config);
            dbContextOptionsBuilder.UseInMemoryDatabase(connStr);
        }
    }
}