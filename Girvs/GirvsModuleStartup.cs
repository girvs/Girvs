namespace Girvs;

public class GirvsModuleStartup : IAppModuleStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<IGirvsPrincipalAccessor, GirvsPrincipalAccessor>();

        // 复用同一个类型查找器，避免每个注册方法各自完整扫描一遍程序集。
        var typeFinder = new WebAppTypeFinder();
        services.RegisterRepository(typeFinder);
        services.RegisterUow();
        services.RegisterManager(typeFinder);
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
#if NET10_0_OR_GREATER
            options.KnownIPNetworks.Clear();
#else
            options.KnownNetworks.Clear();
#endif
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
