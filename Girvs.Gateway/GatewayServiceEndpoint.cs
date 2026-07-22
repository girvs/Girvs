namespace Girvs.Aspire.Gateway;

/// <summary>网关发现到的一个后端服务：服务名 + YARP 目的地集合。</summary>
public sealed class GatewayServiceEndpoint
{
    public required string ServiceName { get; init; }
    public required IReadOnlyDictionary<string, DestinationConfig> Destinations { get; init; }
}
