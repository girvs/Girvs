using Microsoft.AspNetCore.Hosting;

namespace Girvs.EntityFrameworkCore;

public class GirvsEntityFrameworkCoreModule : IAppModuleStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var dbConfig = Singleton<AppSettings>.Instance.Get<DbConfig>();
        // 启动期一次性解析连接串，资源引用配错时立即失败，而不是等到第一次建连
        dbConfig.ResolveConnectionStrings(Singleton<AppSettings>.Instance.Resources);
        services.AddGirvsObjectContext();
        // services.AddGirvsShardingCoreContext();
        services.AddScoped(typeof(IUnitOfWork<>), typeof(UnitOfWork<>));
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
    }

    public void Configure(IApplicationBuilder application, IWebHostEnvironment env)
    {
        // application.ApplicationServices.GetRequiredService<IShardingBootstrapper>().Start();
        // 日志由框架必然注册，用 GetRequiredService 避免 null 引用掩盖真实启动错误。
        var logger = application.ApplicationServices.GetRequiredService<
            ILogger<GirvsEntityFrameworkCoreModule>
        >();
        try
        {
            logger.LogInformation("开始执行数据库还原");
            // 复用容器中已注册的类型查找器，命中其程序集与类型缓存。
            var typeFinder = EngineContext.Current.Resolve<ITypeFinder>() ?? new WebAppTypeFinder();
            var dbContextTypes = typeFinder
                .FindOfType(typeof(GirvsDbContext))
                .Where(x => !x.IsAbstract && !x.IsInterface)
                .ToList();
            if (dbContextTypes.Count == 0)
                return;

            // 模块配置在循环外解析一次，避免每个 DbContext 重复获取。
            var moduleConfig = EngineContext.Current.GetAppModuleConfig<DbConfig>();

            using (var scope = application.ApplicationServices.CreateScope())
            {
                foreach (var dbContextType in dbContextTypes)
                {
                    var dbContext =
                        scope.ServiceProvider.GetRequiredService(dbContextType) as GirvsDbContext;
                    var dbConfig = moduleConfig.GetDataConnectionConfig(dbContextType);

                    if (dbConfig is { EnableAutoMigrate: true })
                    {
                        dbContext?.SwitchReadWriteDataBase(DataBaseWriteAndRead.Write);
                        // 启动期同步上下文，使用 EF 同步迁移 API，避免 sync-over-async 阻塞
                        dbContext?.Database.Migrate();
                    }
                }
            }

            logger.LogInformation(
                "成功执行数据库还原，共处理 {DbContextCount} 个上下文",
                dbContextTypes.Count
            );
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
