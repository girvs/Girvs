using Girvs;
using Microsoft.EntityFrameworkCore;
using Sample.ServiceA.Data;

namespace Sample.ServiceA;

public class Startup(IConfiguration configuration, IWebHostEnvironment env) : IGirvsStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        // HttpClient 工厂：AspireModule 已通过 ConfigureHttpClientDefaults 为所有客户端
        // 启用服务发现与标准弹性，故此处普通客户端即可用 http://sample-serviceb 解析
        services.AddHttpClient();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // 样例免迁移建表：EnableAutoMigrate=false，启动时对样例 DbContext 调 EnsureCreated 建表
        using (var scope = app.ApplicationServices.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
            db.Database.EnsureCreated();
        }

        // 模块端点（AspireModule /health 等）由框架 ConfigureEndpointRouteBuilder 映射；控制器需在此显式映射
        if (app is IEndpointRouteBuilder endpoints)
            endpoints.MapControllers();
    }
}
