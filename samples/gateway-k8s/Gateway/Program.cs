using Girvs.Gateway;
using Girvs.Gateway.FlowProtection;
using Girvs.ServiceGovernance.Configuration;
using Girvs.ServiceGovernance.Discovery;

// kind 集群动态路由验证用最小网关：
// - ServiceGovernance 使用 Kubernetes 服务目录，后台服务按 DiscoveryRefreshInterval 轮询刷新（无需手动启动）
// - Gateway 依据服务目录生成约定路由 /{service}/{**catch-all} 并注册 YARP 反向代理
// - 验证场景缩短刷新间隔为 5 秒，便于观察 Service 增删后的路由变化
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://+:8080");
builder.Configuration.AddJsonFile("appsettings.FlowProtection.json", optional: true, reloadOnChange: true);
builder.Services.AddGirvsServiceDirectory(
    new ServiceGovernanceConfig
    {
        ServiceDiscoveryProvider = ServiceDiscoveryProvider.Kubernetes,
        DiscoveryRefreshInterval = 5,
    }
);

builder.Services.AddGirvsGateway(
    builder.Configuration
);
builder.Services.AddCors(options => options.AddPolicy("gateway", policy => policy
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()
    .WithExposedHeaders(FlowProtectionHeaders.ResponseHeaders)));

var app = builder.Build();

app.UseCors("gateway");
app.MapReverseProxy(proxyPipeline =>
{
    proxyPipeline.UseMiddleware<FlowTicketMiddleware>();
});
app.MapGet("/healthz", () => Results.Ok("ok"));

app.Run();
