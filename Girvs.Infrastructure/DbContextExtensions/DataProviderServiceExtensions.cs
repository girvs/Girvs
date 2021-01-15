using System;
using System.Linq;
using System.Reflection;
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
            var typeFinder = EngineContext.Current.Resolve<ITypeFinder>();
            var dbContexts = typeFinder.FindClassesOfType<GirvsDbContext>().Where(x => x.Name != nameof(GirvsDbContext))
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
                        dmi.Invoke(serviceType, new object[] {services});
                    }
                }
            }

        }

        public static IServiceCollection AddSpDbContext<TContext>(this IServiceCollection services) where TContext : DbContext
        {
            return services.AddDbContext<TContext>(ServiceLifetime.Scoped, ServiceLifetime.Singleton);
        }

        public static IServiceCollection AddSpDbContextPool<TContext>(this IServiceCollection services,
            Action<DbContextOptionsBuilder> optionsAction) where TContext : DbContext
        {
            return services.AddDbContextPool<TContext>(optionsAction);
        }
    }
}