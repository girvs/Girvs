using System;
using System.Linq;
using Girvs.EntityFrameworkCore.Context;
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
            var typeFinder =  new WebAppTypeFinder();
            var dbContexts = typeFinder.FindOfType(typeof
                    (IDbContext)).Where(x => !x.IsAbstract && !x.IsInterface).ToList();

            if (!dbContexts.Any()) return;
            var serviceType = typeof(DataProviderServiceExtensions);
            var mi = serviceType.GetMethod(nameof(AddSpDbContext));
            if (mi == null) return;
            foreach (var dmi in dbContexts.Select(dbContext => mi.MakeGenericMethod(dbContext)))
            {
                dmi.Invoke(serviceType, new object[] {services});
            }
        }

        public static IServiceCollection AddSpDbContext<TContext>(this IServiceCollection services)
            where TContext : DbContext
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