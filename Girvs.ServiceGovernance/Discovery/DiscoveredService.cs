namespace Girvs.ServiceGovernance.Discovery;

/// <summary>服务目录中的一个可路由业务服务。</summary>
public sealed class DiscoveredService
{
    public required string ServiceName { get; init; }

    public required IReadOnlyList<DiscoveredServiceEndpoint> Endpoints { get; init; }

    public bool GatewayEnabled { get; init; }

    public string? GatewayEndpointName { get; init; }
}

/// <summary>服务的一个命名端点实例。</summary>
public sealed class DiscoveredServiceEndpoint
{
    public required string EndpointName { get; init; }

    public required Uri Address { get; init; }

    public string? InstanceId { get; init; }
}
