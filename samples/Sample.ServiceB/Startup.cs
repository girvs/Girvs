using Girvs;

namespace Sample.ServiceB;

public class Startup(IConfiguration configuration, IWebHostEnvironment env) : IGirvsStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // CAP 自动创建其存储表；ServiceB 无业务 DbContext，无需 EnsureCreated。控制器需显式映射。
        if (app is IEndpointRouteBuilder endpoints)
            endpoints.MapControllers();
    }
}
