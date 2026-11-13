namespace Girvs.ServiceGovernance.Discovery;

/// <summary>线程安全的服务目录快照。</summary>
public sealed class ServiceDirectory : IServiceDirectory, IServiceDirectoryHealth
{
    private readonly object _lock = new();
    private IReadOnlyList<DiscoveredService> _services = [];

    public bool HasLoaded { get; private set; }

    public DateTimeOffset? LastSuccessUtc { get; private set; }

    public event Action? ServicesChanged;

    public IReadOnlyList<DiscoveredService> GetServices() => _services;

    public DiscoveredService? GetService(string serviceName) => _services.FirstOrDefault(service =>
        string.Equals(service.ServiceName, serviceName, StringComparison.OrdinalIgnoreCase)
    );

    public bool IsStale(TimeSpan maxStaleDuration) =>
        !LastSuccessUtc.HasValue || DateTimeOffset.UtcNow - LastSuccessUtc.Value > maxStaleDuration;

    public void Publish(IEnumerable<DiscoveredService> services)
    {
        var snapshot = services
            .OrderBy(service => service.ServiceName, StringComparer.OrdinalIgnoreCase)
            .Select(service => new DiscoveredService
            {
                ServiceName = service.ServiceName,
                GatewayEnabled = service.GatewayEnabled,
                GatewayEndpointName = service.GatewayEndpointName,
                Endpoints = service.Endpoints
                    .OrderBy(endpoint => endpoint.EndpointName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(endpoint => endpoint.Address.AbsoluteUri, StringComparer.OrdinalIgnoreCase)
                    .Select(endpoint => new DiscoveredServiceEndpoint
                    {
                        EndpointName = endpoint.EndpointName,
                        Address = endpoint.Address,
                        InstanceId = endpoint.InstanceId,
                    })
                    .ToArray(),
            })
            .ToArray();

        var changed = false;
        lock (_lock)
        {
            changed = !SnapshotsEqual(_services, snapshot);
            _services = snapshot;
            HasLoaded = true;
            LastSuccessUtc = DateTimeOffset.UtcNow;
        }

        if (changed)
            ServicesChanged?.Invoke();
    }

    private static bool SnapshotsEqual(
        IReadOnlyList<DiscoveredService> left,
        IReadOnlyList<DiscoveredService> right
    )
    {
        if (left.Count != right.Count)
            return false;

        for (var index = 0; index < left.Count; index++)
        {
            var leftService = left[index];
            var rightService = right[index];
            if (
                !string.Equals(leftService.ServiceName, rightService.ServiceName, StringComparison.OrdinalIgnoreCase)
                || leftService.GatewayEnabled != rightService.GatewayEnabled
                || !string.Equals(
                    leftService.GatewayEndpointName,
                    rightService.GatewayEndpointName,
                    StringComparison.OrdinalIgnoreCase
                )
                || leftService.Endpoints.Count != rightService.Endpoints.Count
            )
                return false;

            for (var endpointIndex = 0; endpointIndex < leftService.Endpoints.Count; endpointIndex++)
            {
                var leftEndpoint = leftService.Endpoints[endpointIndex];
                var rightEndpoint = rightService.Endpoints[endpointIndex];
                if (
                    !string.Equals(leftEndpoint.EndpointName, rightEndpoint.EndpointName, StringComparison.OrdinalIgnoreCase)
                    || leftEndpoint.Address != rightEndpoint.Address
                    || !string.Equals(leftEndpoint.InstanceId, rightEndpoint.InstanceId, StringComparison.Ordinal)
                )
                    return false;
            }
        }

        return true;
    }
}
