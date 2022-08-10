namespace Girvs.DynamicWebApi;

public class DynamicWebApiModule : IAppModuleStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDynamicWebApi();
    }

    public void Configure(IApplicationBuilder application)
    {
    }

    public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
    {
            
    }

    public int Order { get; } = 4;
}