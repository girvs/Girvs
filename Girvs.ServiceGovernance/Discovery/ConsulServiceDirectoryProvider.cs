namespace Girvs.ServiceGovernance.Discovery;

/// <summary>从 Consul 健康实例目录读取标记为 Girvs 业务服务的实例。</summary>
internal sealed class ConsulServiceDirectoryProvider(ServiceGovernanceConfig config)
    : IServiceDirectoryProvider
{
    private const string BusinessTag = "girvs.business=true";

    public async Task<IReadOnlyList<DiscoveredService>> GetServicesAsync(CancellationToken cancellationToken)
    {
        using var client = new ConsulClient(options => options.Address = new Uri(config.ConsulAddress));
        var catalog = await client.Catalog.Services(cancellationToken);
        var result = new List<DiscoveredService>();
        foreach (var entry in catalog.Response.Where(entry => entry.Value.Contains(BusinessTag)))
        {
            var health = await client.Health.Service(entry.Key, string.Empty, true, cancellationToken);
            var endpoints = health.Response
                .Select(item => new DiscoveredServiceEndpoint
                {
                    EndpointName = GetTagValue(item.Service.Tags, "girvs.endpoint") ?? "http",
                    Address = new Uri($"http://{GetAddress(item)}:{item.Service.Port}"),
                    InstanceId = item.Service.ID,
                })
                .ToArray();
            if (endpoints.Length == 0)
                continue;

            result.Add(
                new DiscoveredService
                {
                    ServiceName = entry.Key,
                    Endpoints = endpoints,
                    GatewayEnabled = string.Equals(
                        GetTagValue(health.Response[0].Service.Tags, "girvs.gateway-enabled"),
                        "true",
                        StringComparison.OrdinalIgnoreCase
                    ),
                    GatewayEndpointName = GetTagValue(
                        health.Response[0].Service.Tags,
                        "girvs.gateway-endpoint"
                    ),
                }
            );
        }
        return result;
    }

    private static string GetAddress(ServiceEntry entry) =>
        string.IsNullOrWhiteSpace(entry.Service.Address) ? entry.Node.Address : entry.Service.Address;

    private static string? GetTagValue(string[]? tags, string key) => tags?
        .FirstOrDefault(tag => tag.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))?
        .Substring(key.Length + 1);
}
