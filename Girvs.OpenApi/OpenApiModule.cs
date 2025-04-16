using Girvs.Infrastructure;
using IGeekFan.AspNetCore.Knife4jUI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

namespace Girvs.OpenApi;

public class OpenApiModule : IAppModuleStartup
{
    private readonly string _documentName = "girvs_webapi_document";

    public OpenApiModule()
    {
        var currentName = AppDomain.CurrentDomain.FriendlyName;
        _documentName = currentName;
    }

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddEndpointsApiExplorer();
        services.AddOpenApi(
            _documentName,
            options =>
            {
                options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            }
        );
    }

    public void Configure(IApplicationBuilder application, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            application.UseSwaggerUI(options =>
            {
                options.RoutePrefix = "girvs_swagger";
                options.SwaggerEndpoint(
                    $"/girvs_openapi/{_documentName}.json",
                    "girvs webapi Docs"
                );
            });

            application.UseKnife4UI(c =>
            {
                c.RoutePrefix = "girvs_knife4"; // serve the UI at root
                c.SwaggerEndpoint($"/girvs_openapi/{_documentName}.json", "girvs webapi Docs");
            });
        }
    }

    public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
    {
        var env = builder.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        if (env.IsDevelopment())
        {
            builder.MapOpenApi(pattern: "/girvs_openapi/{documentName}.json");
            builder.MapScalarApiReference(options =>
            {
                options.EndpointPathPrefix = "/girvs_scalar/{documentName}";
                options.OpenApiRoutePattern = "/girvs_openapi/{documentName}.json";
            });
        }
    }

    public int Order { get; } = 20;
}
