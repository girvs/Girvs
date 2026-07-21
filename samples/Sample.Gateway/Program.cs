using Girvs.Aspire.Gateway;
using Girvs.Aspire.Gateway.Configuration;
using Girvs.Aspire.Gateway.Discovery;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var gatewayDiscovery = builder.Configuration
    .GetSection("GatewayDiscovery")
    .Get<GatewayDiscoveryConfig>()
    ?? new GatewayDiscoveryConfig();

builder.Services.AddGirvsGateway(gatewayDiscovery, builder.Configuration);

var app = builder.Build();

await app.Services.GetRequiredService<IGatewayServiceDiscoverySource>()
    .StartAsync(CancellationToken.None);

app.UseSwaggerUI(options =>
{
    options.RoutePrefix = "girvs_swagger";
    options.DocumentTitle = "Girvs Sample Gateway API Docs";
    options.IndexStream = () => new MemoryStream(
        Encoding.UTF8.GetBytes(
            """
            <!DOCTYPE html>
            <html lang="zh-CN">
            <head>
                <meta charset="UTF-8">
                <title>Girvs Sample Gateway API Docs</title>
                <link rel="stylesheet" type="text/css" href="./swagger-ui.css">
                <link rel="icon" type="image/png" href="./favicon-32x32.png" sizes="32x32">
                <style>html{box-sizing:border-box;overflow-y:scroll}*,*:before,*:after{box-sizing:inherit}body{margin:0;background:#fafafa}</style>
            </head>
            <body>
                <div id="swagger-ui"></div>
                <script src="./swagger-ui-bundle.js"></script>
                <script src="./swagger-ui-standalone-preset.js"></script>
                <script>
                window.onload = async function () {
                    const response = await fetch('./swagger-config', { cache: 'no-store' });
                    const config = await response.json();
                    window.ui = SwaggerUIBundle({
                        dom_id: '#swagger-ui',
                        deepLinking: true,
                        urls: config.urls,
                        presets: [
                            SwaggerUIBundle.presets.apis,
                            SwaggerUIStandalonePreset
                        ],
                        plugins: [
                            SwaggerUIBundle.plugins.DownloadUrl
                        ],
                        layout: 'StandaloneLayout'
                    });
                };
                </script>
            </body>
            </html>
            """
        )
    );
});

app.MapGet("/girvs_swagger/swagger-config", (IGatewayServiceDiscoverySource source) =>
{
    var urls = source.GetServices()
        .OrderBy(service => service.ServiceName, StringComparer.OrdinalIgnoreCase)
        .Select(service => new
        {
            url = $"/{service.ServiceName}/girvs_openapi/girvs_api.json",
            name = $"{service.ServiceName} API"
        })
        .ToArray();

    return Results.Json(new { urls });
});

app.MapReverseProxy();
app.MapGet("/health", () => Results.Ok("ok"));

app.Run();
