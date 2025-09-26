using Microsoft.AspNetCore.Hosting;

namespace Girvs.Quartz;

public class QuartzModule : IAppModuleStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var quartzConfig = Singleton<AppSettings>.Instance.Get<QuartzConfiguration>();
        services.AddQuartzHosted(quartzConfig);
    }

    public void Configure(IApplicationBuilder application, IWebHostEnvironment env) { }

    public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder) { }

    public int Order { get; } = 6;
}
