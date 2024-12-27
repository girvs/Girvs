using Microsoft.AspNetCore.Hosting;

namespace Girvs.EntityFrameworkCore;

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

    public void Configure(IApplicationBuilder application, IWebHostEnvironment env)
    {
        // application.ApplicationServices.GetRequiredService<IShardingBootstrapper>().Start();
        var logger =
            application.ApplicationServices.GetService(typeof(ILogger<object>)) as ILogger<object>;
        try
        {
            logger.LogInformation("开始执行数据库还原");
            var typeFinder = new WebAppTypeFinder();
            var dbContextTypes = typeFinder
                .FindOfType(typeof(GirvsDbContext))
                .Where(x => !x.IsAbstract && !x.IsInterface)
                .ToList();
            if (!dbContextTypes.Any())
                return;

            using (var scope = application.ApplicationServices.CreateScope())
            {
                foreach (var dbContextType in dbContextTypes)
                {
                    var dbContext =
                        scope.ServiceProvider.GetRequiredService(dbContextType) as GirvsDbContext;
                    var dbConfig = EngineContext
                        .Current.GetAppModuleConfig<DbConfig>()
                        .GetDataConnectionConfig(dbContextType);

                    if (dbConfig is { EnableAutoMigrate: true })
                    {
                        dbContext?.SwitchReadWriteDataBase(DataBaseWriteAndRead.Write);
                        dbContext?.Database.MigrateAsync().Wait();
                    }
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

    public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder) { }

    public int Order { get; } = 5;
}
