using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Girvs.Swagger;

public class SwaggerModule : IAppModuleStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSwaggerServices();
    }

    public void Configure(IApplicationBuilder application, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            application.UseSwaggerService();
        }
    }

    public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
    {
        var env = builder.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        if (env.IsDevelopment())
        {
            builder.MapSwagger("{documentName}/api-docs");
        }
    }

    public int Order { get; } = 99902;
}
