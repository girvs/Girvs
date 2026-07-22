using Microsoft.AspNetCore.Hosting;

namespace Girvs.Refit;

public class RefitModule : IAppModuleStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var config = Singleton<AppSettings>.Instance.Get<RefitConfig>() ?? new RefitConfig();
        services.AddSingleton(config);
        services.AddHttpContextAccessor();
        services.AddSingleton<IRefitServiceEndpointResolver, StaticRefitServiceEndpointResolver>();
        if (config.DiscoveryProvider == RefitDiscoveryProvider.Consul)
            services.AddSingleton<IRefitServiceEndpointResolver, ConsulRefitServiceEndpointResolver>();
        else
            services.AddSingleton<IRefitServiceEndpointResolver, AspireRefitServiceEndpointResolver>();

        var refits = new WebAppTypeFinder().FindOfType<IGirvsRefit>(findType: FindType.Interface)
            .Where(x => x.Name != nameof(IGirvsRefit));
        foreach (var refit in refits)
        {
            if (refit.GetCustomAttribute(typeof(RefitServiceAttribute)) is not RefitServiceAttribute service) continue;
            services.AddRefitClient(refit, new RefitSettings(new SystemTextJsonContentSerializer()))
                .ConfigureHttpClient(client => client.BaseAddress = new Uri($"http://{service.ServiceName}"))
                .AddHttpMessageHandler(provider => ActivatorUtilities.CreateInstance<AuthenticatedHttpClientHandler>(provider, service));
        }
    }

    public void Configure(IApplicationBuilder application, IWebHostEnvironment env) { }
    public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder) { }
    public int Order => 100;
}
