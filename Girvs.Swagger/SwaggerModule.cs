﻿namespace Girvs.Swagger;

public class SwaggerModule: IAppModuleStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSwaggerServices();
    }

    public void Configure(IApplicationBuilder application)
    {
        application.UseSwaggerService();
    }

    public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
    {
        builder.MapSwagger("{documentName}/api-docs");
    }

    public int Order { get; } = 99902;
}