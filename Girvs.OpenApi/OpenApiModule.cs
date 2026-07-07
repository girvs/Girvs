using Girvs.Infrastructure;
using IGeekFan.AspNetCore.Knife4jUI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

namespace Girvs.OpenApi;

public class OpenApiModule : IAppModuleStartup
{
    private readonly string _documentName = "girvs_api";

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddEndpointsApiExplorer();
        services.AddOpenApi(
            _documentName,
            options =>
            {
                options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
                options.ShouldInclude = (_) => true;
            }
        );

        // services.Configure<MvcOptions>(options =>
        // {
        //     options.Conventions.Add(new AutoBindingConvention());
        // });
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
#if NET10_0
            // Scalar.AspNetCore 2.x 移除 EndpointPathPrefix，端点路径改为 MapScalarApiReference 首参传入
            // Scalar 2.x 的端点前缀不能包含 {documentName}，文档名通过 AddDocument 绑定。
            builder.MapScalarApiReference("/girvs_scalar", options =>
            {
                options.OpenApiRoutePattern = "/girvs_openapi/{documentName}.json";
                options.AddDocument(
                    _documentName,
                    "girvs webapi Docs",
                    $"/girvs_openapi/{_documentName}.json"
                );
            });
#else
            // Scalar.AspNetCore 1.x（net9）通过 EndpointPathPrefix 设置端点路径
            builder.MapScalarApiReference(options =>
            {
                // Scalar 1.x 通过固定 EndpointPathPrefix 暴露 UI，文档地址直接指向当前 OpenAPI 文档。
                options.EndpointPathPrefix = "/girvs_scalar";
                options.OpenApiRoutePattern = $"/girvs_openapi/{_documentName}.json";
            });
#endif
        }
    }

    public int Order { get; } = 20;
}
