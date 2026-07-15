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
        // 请求管道与端点映射由 CreateGirvsWebApplicationBuilder 统一处理
        // （内部调用 ConfigureRequestPipeline + ConfigureEndpointRouteBuilder，
        //  AspireModule 的 /health、/alive 端点在此自动映射），本壳服务无需额外配置
    }
}
