using System;
using System.Linq;
using Girvs.BusinessBasis.Repositories;
using Girvs.BusinessBasis.UoW;
using Girvs.EntityFrameworkCore.Configuration;
using Girvs.EntityFrameworkCore.Context;
using Girvs.EntityFrameworkCore.DbContextExtensions;
using Girvs.EntityFrameworkCore.Enumerations;
using Girvs.EntityFrameworkCore.Repositories;
using Girvs.EntityFrameworkCore.UoW;
using Girvs.Infrastructure;
using Girvs.TypeFinder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ShardingCore.Bootstrapers;

namespace Girvs.EntityFrameworkCore
{
    public class GirvsEntityFrameworkCoreModule : IAppModuleStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddGirvsObjectContext();
            // services.AddGirvsShardingCoreContext();
            services.AddScoped(typeof(IUnitOfWork<>), typeof(UnitOfWork<>));
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        }

        public void Configure(IApplicationBuilder application)
        {
            // application.ApplicationServices.GetRequiredService<IShardingBootstrapper>().Start();
            var logger = application.ApplicationServices.GetService(typeof(ILogger<object>)) as ILogger<object>;
            try
            {
                logger.LogInformation("开始执行数据库还原");
                var typeFinder = new WebAppTypeFinder();
                var dbContexts = typeFinder.FindOfType(typeof
                    (GirvsDbContext)).Where(x => !x.IsAbstract && !x.IsInterface).ToList();
                if (!dbContexts.Any()) return;


                foreach (var dbContext in dbContexts.Select(dbContextType =>
                    EngineContext.Current.Resolve(dbContextType) as GirvsDbContext))
                {
                    var dbConfig = EngineContext.Current.GetAppModuleConfig<DbConfig>()
                        .GetDataConnectionConfig(dbContext.GetType());

                    if (dbConfig is {EnableAutoMigrate: true})
                    {
                        dbContext?.SwitchReadWriteDataBase(DataBaseWriteAndRead.Write);
                        dbContext?.Database.MigrateAsync().Wait();
                    }
                }

                logger.LogInformation("成功执行数据库还原");
            }
            catch (Exception e)
            {
                logger.LogError(e, "执行数据库还原失败");
            }

            finally
            {
                logger.LogInformation("结束执行数据库还原");
            }
        }

        public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
        {
        }

        public int Order { get; } = 5;
    }
}