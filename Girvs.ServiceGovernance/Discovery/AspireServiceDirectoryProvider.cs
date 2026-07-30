using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Girvs.ServiceGovernance.Discovery;

/// <summary>读取 Aspire AppHost 注入的 services 配置并转换为目录快照。</summary>
internal sealed class AspireServiceDirectoryProvider(
    IConfiguration configuration,
    ServiceGovernanceConfig config,
    ILogger<AspireServiceDirectoryProvider> logger
) : IServiceDirectoryProvider
{
    public Task<IReadOnlyList<DiscoveredService>> GetServicesAsync(CancellationToken cancellationToken) =>
        Task.FromResult(MapConfiguration(configuration, config, logger));

    internal static IReadOnlyList<DiscoveredService> MapConfiguration(
        IConfiguration configuration,
        ServiceGovernanceConfig config,
        Microsoft.Extensions.Logging.ILogger logger
    )
    {
        var result = new List<DiscoveredService>();
        foreach (var serviceSection in configuration.GetSection("services").GetChildren())
        {
            var endpoints = new List<DiscoveredServiceEndpoint>();
            foreach (var endpointSection in serviceSection.GetChildren())
            {
                foreach (var valueSection in endpointSection.GetChildren())
                {
                    if (
                        !Uri.TryCreate(valueSection.Value, UriKind.Absolute, out var address)
                        || (address.Scheme != Uri.UriSchemeHttp && address.Scheme != Uri.UriSchemeHttps)
                    )
                        continue;

                    endpoints.Add(
                        new DiscoveredServiceEndpoint
                        {
                            EndpointName = endpointSection.Key,
                            Address = address,
                            InstanceId = valueSection.Key,
                        }
                    );
                }
            }

            if (endpoints.Count == 0)
                continue;

            config.Services.TryGetValue(serviceSection.Key, out var metadata);
            result.Add(
                new DiscoveredService
                {
                    ServiceName = serviceSection.Key,
                    Endpoints = endpoints,
                    GatewayEnabled = metadata?.GatewayEnabled == true,
                    GatewayEndpointName = metadata?.GatewayEndpointName,
                }
            );
        }

        if (result.Count == 0)
            logger.LogWarning("Aspire 服务目录未发现可路由业务服务");

        return result;
    }
}
