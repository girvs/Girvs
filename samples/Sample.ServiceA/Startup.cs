using Girvs;

namespace Sample.ServiceA;

public class Startup(IConfiguration configuration, IWebHostEnvironment env) : IGirvsStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // CreateGirvsWebApplicationBuilder 传入的 app 即 WebApplication（同时实现 IEndpointRouteBuilder）。
        // 模块端点（AspireModule 的 /health、/alive 等）由框架随后的 ConfigureEndpointRouteBuilder 映射；
        // 普通 MVC 控制器需在此显式映射（与 Girvs 真实服务的 Startup.Configure 一致）。
        if (app is IEndpointRouteBuilder endpoints)
            endpoints.MapControllers();
    }
}
