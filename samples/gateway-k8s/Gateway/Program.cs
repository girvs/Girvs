using Girvs.Gateway;
using Girvs.Gateway.FlowProtection;
using Girvs.ServiceGovernance.Configuration;
using Girvs.ServiceGovernance.Discovery;

// kind 集群 watch 动态路由验证用最小网关：
// - ServiceGovernance 使用 Kubernetes 服务目录，Gateway 注册 YARP 反向代理
// - MapReverseProxy 挂载路由
// - 启动时显式调用 StartAsync 开始 watch（AddGirvsGateway 只注册服务，不自动启动）
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://+:8080");
builder.Configuration.AddJsonFile("appsettings.FlowProtection.json", optional: true, reloadOnChange: true);
builder.Services.AddGirvsServiceDirectory(
    new ServiceGovernanceConfig { ServiceDiscoveryProvider = ServiceDiscoveryProvider.Kubernetes }
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
