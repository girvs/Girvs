using Girvs.Aspire.Gateway;
using Girvs.Aspire.Gateway.Configuration;
using Girvs.Aspire.Gateway.Discovery;

// kind 集群 watch 动态路由验证用最小网关：
// - AddGirvsGateway(K8s) 注册 KubernetesGatewayServiceSource + YARP 反向代理
// - MapReverseProxy 挂载路由
// - 启动时显式调用 StartAsync 开始 watch（AddGirvsGateway 只注册服务，不自动启动）
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://+:8080");

builder.Services.AddGirvsGateway(
    new GatewayDiscoveryConfig { DiscoveryType = GatewayDiscoveryType.Kubernetes }
);

var app = builder.Build();

await app.Services.GetRequiredService<IGatewayServiceDiscoverySource>().StartAsync(CancellationToken.None);

app.MapReverseProxy();
app.MapGet("/healthz", () => Results.Ok("ok"));

app.Run();
