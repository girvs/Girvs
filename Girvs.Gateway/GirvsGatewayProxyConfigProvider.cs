using Girvs.Gateway.Discovery;

namespace Girvs.Gateway;

/// <summary>
/// 依据 <see cref="IGatewayServiceDiscoverySource"/> 的服务快照生成约定式 YARP 路由/集群配置。
/// 修正了旧网关实现中变更令牌未接通的 bug：这里始终先构建携带新令牌的新配置，
/// 再取消“旧”配置持有的令牌，从而让 YARP 在 ChangeToken 触发后重新 GetConfig 拿到新配置。
/// </summary>
public sealed class GirvsGatewayProxyConfigProvider : IProxyConfigProvider, IDisposable
{
    private readonly IGatewayServiceDiscoverySource _source;
    private readonly object _lock = new();
    private volatile GirvsGatewayProxyConfig _config;
    private CancellationTokenSource _cts;
    private bool _disposed;

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
        // 整段读-改-写加锁，避免并发触发（如 K8s watch 重连 relist 在线程池线程重叠）时，
        // 两个线程读到同一个 oldCts 各自 Cancel/Dispose，在已 Dispose 的 CTS 上抛 ObjectDisposedException。
        // 与 Dispose 互斥，避免与销毁流程冲突。
        lock (_lock)
        {
            if (_disposed)
                return;

            // 先建新配置（新令牌），再 Cancel 旧令牌通知 YARP 重新 GetConfig —— 修掉现有令牌未接通的 bug
            var newCts = new CancellationTokenSource();
            var newConfig = BuildConfig(_source.GetServices(), newCts);
            var oldCts = _cts;
            _config = newConfig;
            _cts = newCts;
            oldCts.Cancel();
            oldCts.Dispose();
        }
    }

    private static GirvsGatewayProxyConfig BuildConfig(
        IReadOnlyList<GatewayServiceEndpoint> services,
        CancellationTokenSource cts
    )
    {
        var routes = new List<RouteConfig>();
        var clusters = new List<ClusterConfig>();
        var seen = new HashSet<string>();

        foreach (var svc in services)
        {
            if (!seen.Add(svc.ServiceName))
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
        // 与并发的 OnServicesChanged 互斥，避免在正被切换/取消的 CTS 上重复 Dispose。
        lock (_lock)
        {
            if (_disposed)
                return;
            _disposed = true;
            _cts.Dispose();
        }
    }
}
