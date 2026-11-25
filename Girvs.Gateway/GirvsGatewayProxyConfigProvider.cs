using Girvs.ServiceGovernance.Discovery;
using Yarp.ReverseProxy.Forwarder;

namespace Girvs.Gateway;

/// <summary>
/// 依据 <see cref="IServiceDirectory"/> 的服务快照生成约定式 YARP 路由/集群配置。
/// 修正了旧网关实现中变更令牌未接通的 bug：这里始终先构建携带新令牌的新配置，
/// 再取消“旧”配置持有的令牌，从而让 YARP 在 <see cref="IProxyConfig.ChangeToken"/> 触发后重新
/// <c>GetConfig</c> 拿到新配置。
/// </summary>
/// <remarks>
/// <para>
/// 生命周期：注册为单例并实现 <see cref="IDisposable"/>。构造时订阅服务目录的
/// <c>ServicesChanged</c> 事件，销毁时退订，避免发现源持有已释放的提供程序。
/// </para>
/// <para>
/// 并发模型：配置替换与销毁都在同一把锁内完成。服务发现源可能在多个线程上重复触发变更
/// （例如 K8s watch 断线重连后的 relist 与 watch 事件重叠），锁保证读-改-写整体互斥。
/// 读取方（<see cref="GetConfig"/>）只读 <c>volatile</c> 字段，无锁且总能拿到完整快照。
/// </para>
/// </remarks>
public sealed class GirvsGatewayProxyConfigProvider : IProxyConfigProvider, IDisposable
{
    /// <summary>服务目录，提供当前服务地址快照及变更通知。</summary>
    private readonly IServiceDirectory _directory;

    /// <summary>转发空闲超时，写入每个动态生成集群的请求配置。</summary>
    private readonly TimeSpan _activityTimeout;

    /// <summary>保护配置快照替换与销毁流程的互斥锁。</summary>
    private readonly object _lock = new();

    /// <summary>当前生效的配置快照；由 <c>volatile</c> 保证替换对读取线程立即可见。</summary>
    private volatile GirvsGatewayProxyConfig _config;

    /// <summary>当前快照持有的取消源；取消即触发对应变更令牌，通知 YARP 重新取配置。</summary>
    private CancellationTokenSource _cts;

    /// <summary>是否已销毁，用于同时挡住并发变更与重复销毁。</summary>
    private bool _disposed;

    /// <summary>
    /// 使用默认 <see cref="GirvsGatewayConfig"/>（转发空闲超时 5 分钟）创建提供程序。
    /// </summary>
    /// <param name="directory">服务目录，用于获取服务快照并订阅变更。</param>
    public GirvsGatewayProxyConfigProvider(IServiceDirectory directory)
        : this(directory, new GirvsGatewayConfig())
    {
    }

    /// <summary>
    /// 创建提供程序，并立即用当前服务快照构建首份配置。
    /// </summary>
    /// <param name="directory">服务目录，用于获取服务快照并订阅变更。</param>
    /// <param name="config">网关模块配置，取其 <see cref="GirvsGatewayConfig.ActivityTimeout"/>。</param>
    /// <remarks>
    /// 构造内部只做一次同步构建，尚未启动任何后台循环；服务发现的启动仍由宿主显式调用。
    /// </remarks>
    public GirvsGatewayProxyConfigProvider(IServiceDirectory directory, GirvsGatewayConfig config)
    {
        _directory = directory;
        _activityTimeout = config.ActivityTimeout;
        _cts = new CancellationTokenSource();
        // 首份配置：让 YARP 在第一次 GetConfig 时即有可用路由。
        _config = BuildConfig(directory.GetServices(), _cts, _activityTimeout);
        _directory.ServicesChanged += OnServicesChanged;
    }

    /// <summary>
    /// 获取当前配置快照。YARP 在每次收到变更通知后调用本方法拉取最新路由与集群。
    /// </summary>
    /// <returns>当前生效的不可变配置快照。</returns>
    public IProxyConfig GetConfig() => _config;

    /// <summary>
    /// 服务目录变更回调：在锁内构建新快照并切换，随后取消旧快照的变更令牌。
    /// </summary>
    /// <remarks>
    /// 顺序至关重要：必须先让 <c>_config</c> 指向新配置，再取消旧令牌。若反过来，YARP 可能在被
    /// 通知后立即回调 <see cref="GetConfig"/>，却仍读到旧快照，导致新路由“迟到”到下下次变更。
    /// </remarks>
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
            var newConfig = BuildConfig(_directory.GetServices(), newCts, _activityTimeout);
            var oldCts = _cts;
            _config = newConfig;
            _cts = newCts;
            // 旧令牌先取消（唤醒 YARP 的监听者）再释放，保证尚未观察到的监听者仍能收到通知。
            oldCts.Cancel();
            oldCts.Dispose();
        }
    }

    /// <summary>
    /// 由服务快照构建一份完整的路由/集群配置。
    /// </summary>
    /// <param name="services">当前发现到的服务集合。</param>
    /// <param name="cts">新快照专属的取消源，其令牌将作为配置变更令牌暴露给 YARP。</param>
    /// <param name="activityTimeout">写入每个集群的转发空闲超时。</param>
    /// <returns>包含全部路由、集群与变更令牌的不可变配置快照。</returns>
    /// <remarks>
    /// 生成规则：每个启用了网关且声明了网关端点名的服务，产生
    /// <c>/{服务名}/{**catch-all}</c> 与 <c>/{服务名}</c> 两条路由、一个同名集群；
    /// 转发时通过 <c>PathRemovePrefix</c> 去掉服务名前缀。服务名重复时只取首次出现，
    /// 避免同一集群被重复定义导致 YARP 配置校验失败。
    /// </remarks>
    private static GirvsGatewayProxyConfig BuildConfig(
        IReadOnlyList<DiscoveredService> services,
        CancellationTokenSource cts,
        TimeSpan activityTimeout
    )
    {
        var routes = new List<RouteConfig>();
        var clusters = new List<ClusterConfig>();
        // 记录已处理的服务名，保证一服务一路由，后出现的同名项直接跳过。
        var seen = new HashSet<string>();

        foreach (var svc in services.Where(service =>
            service.GatewayEnabled && !string.IsNullOrWhiteSpace(service.GatewayEndpointName)
        ))
        {
            if (!seen.Add(svc.ServiceName))
                continue;

            // 只保留与该服务声明的网关端点名一致的目标；字典键带序号，避免同端点多实例键冲突。
            var destinations = svc.Endpoints
                .Where(endpoint => string.Equals(
                    endpoint.EndpointName,
                    svc.GatewayEndpointName,
                    StringComparison.OrdinalIgnoreCase
                ))
                .Select((endpoint, index) => new { endpoint, index })
                .ToDictionary(
                    item => $"{svc.ServiceName}-{item.endpoint.EndpointName}-{item.index}",
                    item => new DestinationConfig { Address = item.endpoint.Address.AbsoluteUri }
                );
            // 没有可用目标地址的服务不生成路由，否则 YARP 会因集群无目标而报错。
            if (destinations.Count == 0)
                continue;

            // 约定：客户端路径以服务名开头，转发前剥掉该前缀。
            var transforms = new List<IReadOnlyDictionary<string, string>>
            {
                new Dictionary<string, string> { ["PathRemovePrefix"] = svc.ServiceName }
            };

            routes.Add(
                new RouteConfig
                {
                    RouteId = svc.ServiceName,
                    ClusterId = svc.ServiceName,
                    Match = new RouteMatch { Path = $"/{svc.ServiceName}/" + "{**catch-all}" },
                    Transforms = transforms
                }
            );

            // "/{service}/{**catch-all}" 中 "/" 是必需字面量，请求 "/{service}"（无尾斜杠）
            // 不会匹配。额外补一条根路径路由，保证服务根地址可代理。
            routes.Add(
                new RouteConfig
                {
                    RouteId = $"{svc.ServiceName}-root",
                    ClusterId = svc.ServiceName,
                    Match = new RouteMatch { Path = $"/{svc.ServiceName}" },
                    Transforms = transforms
                }
            );

            clusters.Add(new ClusterConfig
            {
                ClusterId = svc.ServiceName,
                Destinations = destinations,
                HttpRequest = new ForwarderRequestConfig { ActivityTimeout = activityTimeout }
            });
        }

        // 令牌取自本次新建的取消源：只有本快照被替换时才会触发，天然与快照一一对应。
        return new GirvsGatewayProxyConfig(
            routes,
            clusters,
            new CancellationChangeToken(cts.Token)
        );
    }

    /// <summary>
    /// 退订服务目录变更并释放变更令牌，结束本提供程序的生命周期。
    /// </summary>
    /// <remarks>
    /// 先退订再在锁内释放取消源，确保退订后不再有新的回调进入替换流程；
    /// 重复调用是安全的（内部以 <c>_disposed</c> 做幂等判断）。
    /// </remarks>
    public void Dispose()
    {
        _directory.ServicesChanged -= OnServicesChanged;
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
