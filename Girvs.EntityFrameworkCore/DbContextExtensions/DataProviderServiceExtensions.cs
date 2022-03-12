using System;
using System.Linq;
using Girvs.EntityFrameworkCore.Configuration;
using Girvs.EntityFrameworkCore.Context;
using Girvs.Infrastructure;
using Girvs.TypeFinder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.EntityFrameworkCore.DbContextExtensions
{
    public static class DataProviderServiceExtensions
    {
        /// <summary>
        /// 注册数据库基础对象上下文
        /// </summary>
        public static void AddGirvsObjectContext(this IServiceCollection services)
        {
            var typeFinder = new WebAppTypeFinder();
            var dbContexts = typeFinder.FindOfType(typeof
                (GirvsDbContext)).Where(x => !x.IsAbstract && !x.IsInterface).ToList();

            if (!dbContexts.Any()) return;
            var serviceType = typeof(DataProviderServiceExtensions);
            var mi = serviceType.GetMethod(nameof(AddGirvsDbContext));
            if (mi == null) return;
            foreach (var dbContext in dbContexts)
            {
                var dmi = mi.MakeGenericMethod(dbContext);
                var config = EngineContext.Current.GetAppModuleConfig<DbConfig>()?.GetDataConnectionConfig(dbContext);
                Action<IServiceProvider, DbContextOptionsBuilder> optionsAction = (provider, builder) =>
                {
                    builder.ConfigDbContextOptionsBuilder(config,
                        config?.GetSecureRandomReadDataConnectionString());
                };
                dmi.Invoke(serviceType, new object[] {services, optionsAction});
            }
        }

        public static IServiceCollection AddGirvsDbContext<TContext>(this IServiceCollection services,
            Action<IServiceProvider, DbContextOptionsBuilder> optionsAction)
            where TContext : DbContext
        {
            return services.AddDbContext<TContext>(optionsAction, ServiceLifetime.Scoped, ServiceLifetime.Scoped);
        }
    }
}