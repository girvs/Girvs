using System.Collections.Immutable;
using k8s;
using k8s.Models;

namespace Girvs.Aspire.Gateway.Discovery;

/// <summary>基于 K8s Service 的网关服务发现源：relist 建初态 + watch 增量触发重拉映射，断线自动重连。</summary>
public sealed class KubernetesGatewayServiceSource : IGatewayServiceDiscoverySource, IDisposable
{
    private readonly IKubernetes _client;
    private volatile IReadOnlyList<GatewayServiceEndpoint> _services = new List<GatewayServiceEndpoint>();
    private readonly CancellationTokenSource _stop = new();
    private Task? _watchLoop;

    public KubernetesGatewayServiceSource(IKubernetes? client = null)
    {
        // 集群内运行用 InClusterConfig；client 参数便于测试替身注入
        _client = client ?? new Kubernetes(KubernetesClientConfiguration.InClusterConfig());
    }

    public event Action? ServicesChanged;

    public IReadOnlyList<GatewayServiceEndpoint> GetServices() => _services;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _watchLoop = Task.Run(() => WatchLoopAsync(_stop.Token), _stop.Token);
        return Task.CompletedTask;
    }

    private async Task WatchLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // 全量 relist 建立当前状态
                var list = await _client.CoreV1.ListServiceForAllNamespacesAsync(cancellationToken: ct);
                _services = MapServices(list.Items);
                ServicesChanged?.Invoke();

                // 从 relist 的 resourceVersion 起 watch 增量变化
                using var watcher = _client.CoreV1.ListServiceForAllNamespacesWithHttpMessagesAsync(
                    watch: true,
                    resourceVersion: list.Metadata.ResourceVersion,
                    cancellationToken: ct
                );
                await foreach (var (_, _) in watcher.WatchAsync<V1Service, V1ServiceList>(cancellationToken: ct))
                {
                    // 任一 Service 变化后重新全量拉取并映射（简单可靠，服务数量级下开销可接受）
                    var current = await _client.CoreV1.ListServiceForAllNamespacesAsync(cancellationToken: ct);
                    _services = MapServices(current.Items);
                    ServicesChanged?.Invoke();
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // 取消场景（如 Dispose 提前释放 client 触发 ObjectDisposedException）静默退出，
                // 不打重连日志、不进 Delay，避免关闭噪声与无人观察的 faulted task
                if (ct.IsCancellationRequested)
                    break;
                Console.Error.WriteLine($"[Girvs.Aspire.Gateway] K8s watch 中断，2s 后重连：{ex.Message}");
                await Task.Delay(TimeSpan.FromSeconds(2), ct);
            }
        }
    }

    internal static List<GatewayServiceEndpoint> MapServices(IEnumerable<V1Service> services)
    {
        var result = new List<GatewayServiceEndpoint>();
        foreach (var service in services)
        {
            var serviceName = service.Metadata.Name;
            if (result.Exists(x => x.ServiceName == serviceName))
                continue;

            var builder = ImmutableDictionary.CreateBuilder<string, DestinationConfig>();
            foreach (var port in service.Spec?.Ports ?? new List<V1ServicePort>())
            {
                builder[$"{serviceName}-{port.Port}"] = new DestinationConfig
                {
                    Address =
                        $"http://{serviceName}.{service.Metadata.NamespaceProperty}.svc.cluster.local:{port.Port}"
                };
            }

            result.Add(
                new GatewayServiceEndpoint { ServiceName = serviceName, Destinations = builder.ToImmutable() }
            );
        }
        return result;
    }

    public void Dispose()
    {
        _stop.Cancel();
        // 先等 watch 循环收尾（最多 5s），避免在循环仍卡在 HTTP 调用时提前释放 client
        // 导致 ObjectDisposedException 落入重连分支产生关闭噪声
        try
        {
            _watchLoop?.Wait(TimeSpan.FromSeconds(5));
        }
        catch (AggregateException) { }
        catch (OperationCanceledException) { }

        _stop.Dispose();
        (_client as IDisposable)?.Dispose();
    }
}
