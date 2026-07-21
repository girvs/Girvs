using Girvs.Aspire.Gateway;
using Girvs.Aspire.Gateway.Configuration;
using Girvs.Aspire.Gateway.Discovery;
using Girvs.Infrastructure.Extensions;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerUI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureApplicationServices(builder.Configuration, builder.Environment);

var gatewayDiscovery = builder.Configuration
    .GetSection("GatewayDiscovery")
    .Get<GatewayDiscoveryConfig>()
    ?? new GatewayDiscoveryConfig();

builder.Services.AddGirvsGateway(gatewayDiscovery, builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc(
        "v1",
        new OpenApiInfo { Title = AppDomain.CurrentDomain.FriendlyName, Version = "v1" }
    );
});

var app = builder.Build();

var gatewayServiceDiscoverySource = app.Services.GetRequiredService<IGatewayServiceDiscoverySource>();
var swaggerEndpoints = new SwaggerEndpointEnumerator();

await gatewayServiceDiscoverySource.StartAsync(CancellationToken.None);
swaggerEndpoints.Refresh(gatewayServiceDiscoverySource);

app.UseMiddleware<SwaggerFilterMiddleware>(swaggerEndpoints);
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.RoutePrefix = "girvs_swagger";
    c.DocumentTitle = "Girvs Sample Gateway API Docs";
    c.ConfigObject.Urls = swaggerEndpoints;
});

app.MapGet(
    "/",
    async context =>
    {
        await context.Response.WriteAsync("Welcome to ScsApiGateway!");
    }
);

if (app.Environment.IsDevelopment())
{
    app.MapSwagger("{documentName}/api-docs");
}


app.MapReverseProxy();
app.MapGet("/health", () => Results.Ok("ok"));

app.Run();

public sealed class SwaggerEndpointEnumerator : List<UrlDescriptor>
{
    public void Refresh(IGatewayServiceDiscoverySource source)
    {
        Clear();

        AddRange(
            source.GetServices()
                .OrderBy(service => service.ServiceName, StringComparer.OrdinalIgnoreCase)
                .Select(service => new UrlDescriptor
                {
                    Url = $"/{service.ServiceName}/girvs_openapi/girvs_api.json",
                    Name = $"{service.ServiceName} API"
                })
        );
    }
}

public sealed class SwaggerFilterMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SwaggerEndpointEnumerator _swaggerEndpoints;

    public SwaggerFilterMiddleware(
        RequestDelegate next,
        SwaggerEndpointEnumerator swaggerEndpoints)
    {
        _next = next;
        _swaggerEndpoints = swaggerEndpoints;
    }

    public Task InvokeAsync(
        HttpContext context,
        IGatewayServiceDiscoverySource source)
    {
        _swaggerEndpoints.Refresh(source);
        return _next(context);
    }
}
