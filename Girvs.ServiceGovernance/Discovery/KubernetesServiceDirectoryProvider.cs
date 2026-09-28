using k8s;
using k8s.Models;

namespace Girvs.ServiceGovernance.Discovery;

/// <summary>从带 Girvs 业务标签的 Kubernetes Service 构建集群内可路由地址。</summary>
internal sealed class KubernetesServiceDirectoryProvider(ServiceGovernanceConfig config)
    : IServiceDirectoryProvider,
        IDisposable
{
    private readonly IKubernetes _client = new Kubernetes(KubernetesClientConfiguration.InClusterConfig());

    public async Task<IReadOnlyList<DiscoveredService>> GetServicesAsync(CancellationToken cancellationToken)
    {
        V1ServiceList list;
        if (string.IsNullOrWhiteSpace(config.KubernetesNamespace))
        {
            list = await _client.CoreV1.ListServiceForAllNamespacesAsync(
                labelSelector: config.KubernetesLabelSelector,
                cancellationToken: cancellationToken
            );
        }
        else
        {
            list = await _client.CoreV1.ListNamespacedServiceAsync(
                config.KubernetesNamespace,
                labelSelector: config.KubernetesLabelSelector,
                cancellationToken: cancellationToken
            );
        }

        return list.Items.Select(MapService).Where(service => service is not null).Cast<DiscoveredService>().ToArray();
    }

    internal static DiscoveredService? MapService(V1Service service)
    {
        var name = service.Metadata.Name;
        var serviceNamespace = service.Metadata.NamespaceProperty;
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(serviceNamespace))
            return null;

        var endpoints = (service.Spec?.Ports ?? [])
            .Select(port => new DiscoveredServiceEndpoint
            {
                EndpointName = string.IsNullOrWhiteSpace(port.Name) ? $"port-{port.Port}" : port.Name,
                Address = new Uri($"http://{name}.{serviceNamespace}.svc.cluster.local:{port.Port}"),
            })
            .ToArray();
        if (endpoints.Length == 0)
            return null;

        string? gatewayEnabled = null;
        string? gatewayEndpoint = null;
        service.Metadata.Annotations?.TryGetValue("girvs.io/gateway-enabled", out gatewayEnabled);
        service.Metadata.Annotations?.TryGetValue("girvs.io/gateway-endpoint", out gatewayEndpoint);
        return new DiscoveredService
        {
            ServiceName = name,
            Endpoints = endpoints,
            GatewayEnabled = string.Equals(gatewayEnabled, "true", StringComparison.OrdinalIgnoreCase),
            GatewayEndpointName = gatewayEndpoint,
        };
    }

    public void Dispose() => _client.Dispose();
}
