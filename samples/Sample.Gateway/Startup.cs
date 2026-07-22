using Girvs;
using Girvs.Gateway;
using Girvs.Gateway.Configuration;
using Girvs.Gateway.Discovery;
using Microsoft.OpenApi;

namespace Sample.Gateway;

public class Startup : IGirvsStartup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration, IWebHostEnvironment env)
    {
        _configuration = configuration;
        _ = env;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        var gatewayDiscovery = _configuration
            .GetSection("GatewayDiscovery")
            .Get<GatewayDiscoveryConfig>()
            ?? new GatewayDiscoveryConfig();

        services.AddGirvsGateway(gatewayDiscovery, _configuration);
        services.AddSingleton<SwaggerEndpointEnumerator>();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc(
                "v1",
                new OpenApiInfo { Title = AppDomain.CurrentDomain.FriendlyName, Version = "v1" }
            );
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        var gatewayServiceDiscoverySource =
            app.ApplicationServices.GetRequiredService<IGatewayServiceDiscoverySource>();
        var swaggerEndpoints =
            app.ApplicationServices.GetRequiredService<SwaggerEndpointEnumerator>();

        gatewayServiceDiscoverySource.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
        swaggerEndpoints.Refresh(gatewayServiceDiscoverySource);

        app.UseMiddleware<SwaggerFilterMiddleware>();
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.RoutePrefix = "girvs_swagger";
            c.DocumentTitle = "Girvs Sample Gateway API Docs";
            c.ConfigObject.Urls = swaggerEndpoints;
        });

        if (app is not IEndpointRouteBuilder endpoints)
            return;

        endpoints.MapGet(
            "/",
            async context =>
            {
                await context.Response.WriteAsync("Welcome to ScsApiGateway!");
            }
        );

        if (env.IsDevelopment())
        {
            endpoints.MapSwagger("{documentName}/api-docs");
        }

        endpoints.MapReverseProxy();
        endpoints.MapGet("/health", () => Results.Ok("ok"));
    }
}
