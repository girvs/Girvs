using Girvs.Aspire.Gateway.Discovery;

namespace Girvs.Aspire.Gateway;

/// <summary>
/// 依据 <see cref="IGatewayServiceDiscoverySource"/> 的服务快照生成约定式 YARP 路由/集群配置。
/// 修正了旧网关实现中变更令牌未接通的 bug：这里始终先构建携带新令牌的新配置，
/// 再取消“旧”配置持有的令牌，从而让 YARP 在 ChangeToken 触发后重新 GetConfig 拿到新配置。
/// </summary>
public sealed class GirvsGatewayProxyConfigProvider : IProxyConfigProvider, IDisposable
{
    private readonly IGatewayServiceDiscoverySource _source;
    private volatile GirvsGatewayProxyConfig _config;
    private CancellationTokenSource _cts;

    public GirvsGatewayProxyConfigProvider(IGatewayServiceDiscoverySource source)
    {
        _source = source;
        _cts = new CancellationTokenSource();
        _config = BuildConfig(source.GetServices(), _cts);
        _source.ServicesChanged += OnServicesChanged;
    }

    public IProxyConfig GetConfig() => _config;

    private void OnServicesChanged()
    {
        // 先建新配置（新令牌），再 Cancel 旧令牌通知 YARP 重新 GetConfig —— 修掉现有令牌未接通的 bug
        var newCts = new CancellationTokenSource();
        var newConfig = BuildConfig(_source.GetServices(), newCts);
        var oldCts = _cts;
        _config = newConfig;
        _cts = newCts;
        oldCts.Cancel();
        oldCts.Dispose();
    }

    private static GirvsGatewayProxyConfig BuildConfig(
        IReadOnlyList<GatewayServiceEndpoint> services,
        CancellationTokenSource cts
    )
    {
        var routes = new List<RouteConfig>();
        var clusters = new List<ClusterConfig>();

        foreach (var svc in services)
        {
            if (routes.Exists(r => r.RouteId == svc.ServiceName))
                continue;

            routes.Add(
                new RouteConfig
                {
                    RouteId = svc.ServiceName,
                    ClusterId = svc.ServiceName,
                    Match = new RouteMatch { Path = $"/{svc.ServiceName}/" + "{**catch-all}" },
                    Transforms = new List<IReadOnlyDictionary<string, string>>
                    {
                        new Dictionary<string, string> { ["PathRemovePrefix"] = svc.ServiceName }
                    }
                }
            );

            clusters.Add(
                new ClusterConfig { ClusterId = svc.ServiceName, Destinations = svc.Destinations }
            );
        }

        return new GirvsGatewayProxyConfig(
            routes,
            clusters,
            new CancellationChangeToken(cts.Token)
        );
    }

    public void Dispose()
    {
        _source.ServicesChanged -= OnServicesChanged;
        _cts.Dispose();
    }
}
