using Girvs.Aspire.Gateway;
using Girvs.Aspire.Gateway.Configuration;
using Girvs.Aspire.Gateway.Discovery;

var builder = WebApplication.CreateBuilder(args);

var consulAddress = builder.Configuration["ConsulAddress"]
    ?? builder.Configuration["ModuleConfigurations:ConsulConfig:ConsulAddress"]
    ?? "http://127.0.0.1:8500";

builder.Services.AddGirvsGateway(
    new GatewayDiscoveryConfig
    {
        DiscoveryType = GatewayDiscoveryType.Consul,
        ConsulAddress = consulAddress
    },
    builder.Configuration
);

var app = builder.Build();

await app.Services.GetRequiredService<IGatewayServiceDiscoverySource>()
    .StartAsync(CancellationToken.None);

app.MapReverseProxy();
app.MapGet("/health", () => Results.Ok("ok"));

app.Run();
