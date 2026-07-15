using Girvs;

namespace Sample.Worker;

public class Startup(IConfiguration configuration, IWebHostEnvironment env) : IGirvsStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddHostedService<HeartbeatWorker>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (app is IEndpointRouteBuilder endpoints)
            endpoints.MapControllers();
    }
}
