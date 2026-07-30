using Microsoft.AspNetCore.Hosting;

namespace Girvs.Refit;

public class RefitModule : IAppModuleStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var config = Singleton<AppSettings>.Instance.Get<RefitConfig>() ?? new RefitConfig();
        services.AddSingleton(config);
        services.AddHttpContextAccessor();

#if NET10_0
        var serviceGovernanceConfig = Singleton<AppSettings>.Instance.Get<ServiceGovernanceConfig>();
        RegisterEndpointResolvers(services, config, serviceGovernanceConfig);
#else
        RegisterEndpointResolvers(services, config);
#endif

        // 只扫描显式标注 RefitServiceAttribute 的业务接口。
        var refits = new WebAppTypeFinder().FindOfType<IGirvsRefit>(findType: FindType.Interface)
            .Where(x => x.Name != nameof(IGirvsRefit));
        foreach (var refit in refits)
        {
            if (refit.GetCustomAttribute(typeof(RefitServiceAttribute)) is not RefitServiceAttribute service) continue;
            // Refit 的路由通常是相对路径，因此统一提供逻辑服务基地址。
            // Aspire 模式会在请求发送时将该地址解析为实际端点。
            services.AddRefitClient(refit, new RefitSettings(new SystemTextJsonContentSerializer()))
                .ConfigureHttpClient(client => client.BaseAddress = new Uri($"http://{service.ServiceName}"))
                .AddHttpMessageHandler(provider => ActivatorUtilities.CreateInstance<AuthenticatedHttpClientHandler>(provider, service));
        }
    }

#if NET10_0
    internal static void RegisterEndpointResolvers(
        IServiceCollection services,
        RefitConfig config,
        ServiceGovernanceConfig serviceGovernanceConfig
    )
    {
        services.AddSingleton<IRefitServiceEndpointResolver, StaticRefitServiceEndpointResolver>();
        if (serviceGovernanceConfig.ServiceDiscoveryProvider == ServiceDiscoveryProvider.Consul)
            services.AddSingleton<IRefitServiceEndpointResolver, ConsulRefitServiceEndpointResolver>();
        else
            services.AddSingleton<IRefitServiceEndpointResolver, AspireRefitServiceEndpointResolver>();
    }
#else
    internal static void RegisterEndpointResolvers(
        IServiceCollection services,
        RefitConfig config
    )
    {
        services.AddSingleton<IRefitServiceEndpointResolver, StaticRefitServiceEndpointResolver>();
        if (config.DiscoveryProvider == RefitDiscoveryProvider.Consul)
            services.AddSingleton<IRefitServiceEndpointResolver, ConsulRefitServiceEndpointResolver>();
        else
            services.AddSingleton<IRefitServiceEndpointResolver, AspireRefitServiceEndpointResolver>();
    }
#endif

    public void Configure(IApplicationBuilder application, IWebHostEnvironment env) { }
    public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder) { }
    public int Order => 100;
}
