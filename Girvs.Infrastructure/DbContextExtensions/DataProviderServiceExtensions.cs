using System;
using System.Linq;
using System.Reflection;
using Girvs.Domain;
using Girvs.Domain.Configuration;
using Girvs.Domain.Infrastructure;
using Girvs.Domain.TypeFinder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.Infrastructure.DbContextExtensions
{
    public static class DataProviderServiceExtensions
    {
        /// <summary>
        /// 注册数据库基础对象上下文
        /// </summary>
        public static void AddSpObjectContext(this IServiceCollection services)
        {
            var config = EngineContext.Current.Resolve<GirvsConfig>();
            Action<DbContextOptionsBuilder> optionsAction = builder =>
            {
                switch (config.UseDataType)
                {
                    case UseDataType.MsSql:
                        builder.UseSqlServerWithLazyLoading(services);
                        break;

                    case UseDataType.MySql:
                        builder.UseMySqlWithLazyLoading(services);
                        break;

                    case UseDataType.SqlLite:
                        builder.UseSqlLiteWithLazyLoading(services);
                        break;
                }

                //所有查询不使用数据追踪的方式，以免引起难以想象的麻烦
                if (!config.UseDataTracking)
                {
                    builder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                }

                builder.EnableSensitiveDataLogging();
            };
            
            

            var typeFinder = EngineContext.Current.Resolve<ITypeFinder>();
            var dbContexts = typeFinder.FindClassesOfType<ScsDbContext>().Where(x => x.Name != nameof(ScsDbContext))
                .ToList();

            if (dbContexts.Any())
            {
                Type serviceType = typeof(DataProviderServiceExtensions);
                MethodInfo mi = serviceType.GetMethod(nameof(AddSpDbContext));
                if (mi != null)
                {
                    foreach (var dbContext in dbContexts)
                    {
                        MethodInfo dmi = mi.MakeGenericMethod(dbContext);
                        dmi.Invoke(serviceType, new object[] {services, optionsAction});
                    }
                }
            }
        }

        public static IServiceCollection AddSpDbContext<TContext>(this IServiceCollection services,
            Action<DbContextOptionsBuilder> optionsAction) where TContext : DbContext
        {
            return services.AddDbContext<TContext>(optionsAction, ServiceLifetime.Scoped, ServiceLifetime.Singleton);
        }

        public static IServiceCollection AddSpDbContextPool<TContext>(this IServiceCollection services,
            Action<DbContextOptionsBuilder> optionsAction) where TContext : DbContext
        {
            return services.AddDbContextPool<TContext>(optionsAction);
        }
    }
}