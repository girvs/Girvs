namespace Girvs;

public class GirvsModuleStartup : IAppModuleStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<IGirvsPrincipalAccessor, GirvsPrincipalAccessor>();
        services.RegisterRepository();
        services.RegisterUow();
        services.RegisterManager();
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });
    }

    public void Configure(IApplicationBuilder application, IWebHostEnvironment env)
    {
        application.UseForwardedHeaders(
            new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            }
        );
    }

    public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder) { }

    public int Order { get; } = 0;
}
