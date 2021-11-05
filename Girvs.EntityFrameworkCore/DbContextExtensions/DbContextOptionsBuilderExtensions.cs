using System;
using Girvs.EntityFrameworkCore.Configuration;
using Microsoft.EntityFrameworkCore;
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

            optionsBuilder.UseMySql(connStr, serverVersion);
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
    }
}