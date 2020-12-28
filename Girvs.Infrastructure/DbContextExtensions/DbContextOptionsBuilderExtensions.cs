using Girvs.Domain.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.Infrastructure.DbContextExtensions
{
    public static class DbContextOptionsBuilderExtensions
    {
        private static DbContextOptionsBuilder UseLazyLoading(DbContextOptionsBuilder optionsBuilder, GirvsConfig girvsConfig)
        {
            if (girvsConfig.UseLazyLoading)
            {
                return optionsBuilder.UseLazyLoadingProxies();
            }

            return optionsBuilder;
        }

        public static void UseSqlServerWithLazyLoading(this DbContextOptionsBuilder optionsBuilder,
            IServiceCollection services)
        {
            var girvsConfig = services.BuildServiceProvider().GetRequiredService<GirvsConfig>();
            var dbContextOptionsBuilder = UseLazyLoading(optionsBuilder, girvsConfig);
            dbContextOptionsBuilder.UseSqlServer(girvsConfig.DataConnectionString);
        }

        public static void UseMySqlWithLazyLoading(this DbContextOptionsBuilder optionsBuilder,
            IServiceCollection services)
        {
            var girvsConfig = services.BuildServiceProvider().GetRequiredService<GirvsConfig>();
            var dbContextOptionsBuilder = UseLazyLoading(optionsBuilder, girvsConfig);

            if (girvsConfig.UseRowNumberForPaging)
                dbContextOptionsBuilder.UseMySql(girvsConfig.DataConnectionString,
                    option => { option.CommandTimeout(girvsConfig.SQLCommandTimeout); });
            //dbContextOptionsBuilder.UseMySQL(spConfig.DataConnectionString, option => option.CommandTimeout(spConfig.SQLCommandTimeout));
            else
                dbContextOptionsBuilder.UseSqlServer(girvsConfig.DataConnectionString,
                    option => option.CommandTimeout(girvsConfig.SQLCommandTimeout));
        }

        public static void UseSqlLiteWithLazyLoading(this DbContextOptionsBuilder optionsBuilder,
            IServiceCollection services)
        {
            var girvsConfig = services.BuildServiceProvider().GetRequiredService<GirvsConfig>();

            var dbContextOptionsBuilder = UseLazyLoading(optionsBuilder, girvsConfig);

            if (girvsConfig.UseRowNumberForPaging)
                dbContextOptionsBuilder.UseSqlite(girvsConfig.DataConnectionString,
                    option => option.CommandTimeout(girvsConfig.SQLCommandTimeout));
            else
                dbContextOptionsBuilder.UseSqlServer(girvsConfig.DataConnectionString,
                    option => option.CommandTimeout(girvsConfig.SQLCommandTimeout));
        }

        public static void UseOracleWithLazyLoading(this DbContextOptionsBuilder optionsBuilder,
            IServiceCollection services)
        {
            var girvsConfig = services.BuildServiceProvider().GetRequiredService<GirvsConfig>();

            var dbContextOptionsBuilder = UseLazyLoading(optionsBuilder, girvsConfig);

            dbContextOptionsBuilder.UseOracle(girvsConfig.DataConnectionString,
                option => option.CommandTimeout(girvsConfig.SQLCommandTimeout));
        }

        public static void UseInMemoryWithLazyLoading(this DbContextOptionsBuilder optionsBuilder,
            IServiceCollection services)
        {
            var girvsConfig = services.BuildServiceProvider().GetRequiredService<GirvsConfig>();
            var dbContextOptionsBuilder = UseLazyLoading(optionsBuilder, girvsConfig);
            dbContextOptionsBuilder.UseInMemoryDatabase(girvsConfig.DataConnectionString);
        }
    }
}