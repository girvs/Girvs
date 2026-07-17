namespace Girvs.EntityFrameworkCore.DbContextExtensions;

public static class DataProviderServiceExtensions
{
    /// <summary>
    /// 注册数据库基础对象上下文
    /// </summary>
    public static void AddGirvsObjectContext(this IServiceCollection services)
    {
        var typeFinder = new WebAppTypeFinder();
        var dbContexts = typeFinder
            .FindOfType(typeof(GirvsDbContext))
            .Where(x => !x.IsAbstract && !x.IsInterface)
            .ToList();

        if (!dbContexts.Any())
            return;
        var serviceType = typeof(DataProviderServiceExtensions);
        var mi = serviceType.GetMethod(nameof(AddGirvsDbContext));
        if (mi == null)
            return;
        foreach (var dbContext in dbContexts)
        {
            var dmi = mi.MakeGenericMethod(dbContext);
            var config = EngineContext
                .Current.GetAppModuleConfig<DbConfig>()
                ?.GetDataConnectionConfig(dbContext);
            dmi.Invoke(serviceType, [services, config]);
        }
    }


    public static IServiceCollection AddGirvsDbContext<TContext>(
        this IServiceCollection services,
        DataConnectionConfig config
    )
        where TContext : GirvsDbContext
    {
        return services.AddDbContext<TContext>(
            (provider, builder) =>
            {
                // 用回调自带的作用域 provider 解析:GirvsEngine 在模块注册前生成的快照容器
                // 不含 IDataConnectionStringProvider,经 EngineContext 解析会得到 null
                builder
                    .AddInterceptors(new MySqlSyntaxInterceptor())
                    .ConfigDbContextOptionsBuilder<TContext>(
                        config,
                        provider.GetRequiredService<IDataConnectionStringProvider>()
                            .GetReadConnectionString(config.Name)
                    );
            },
            ServiceLifetime.Scoped,
            ServiceLifetime.Scoped
        );
    }
}
