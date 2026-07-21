using System.Collections.Immutable;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Girvs.Aspire.Gateway.Discovery;

/// <summary>基于 Aspire AppHost 注入配置的本地网关服务发现源。</summary>
public sealed class AspireGatewayServiceSource : IGatewayServiceDiscoverySource
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AspireGatewayServiceSource> _logger;
    private volatile IReadOnlyList<GatewayServiceEndpoint> _services = [];

    public AspireGatewayServiceSource(
        IConfiguration configuration,
        ILogger<AspireGatewayServiceSource> logger
    )
    {
        _configuration = configuration;
        _logger = logger;
    }

    public event Action? ServicesChanged;

    public IReadOnlyList<GatewayServiceEndpoint> GetServices() => _services;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _services = MapConfiguration(_configuration, _logger);
        ServicesChanged?.Invoke();
        return Task.CompletedTask;
    }

    internal static IReadOnlyList<GatewayServiceEndpoint> MapConfiguration(
        IConfiguration configuration,
        ILogger logger
    )
    {
        var result = new List<GatewayServiceEndpoint>();
        foreach (var serviceSection in configuration.GetSection("services").GetChildren())
        {
            var destinations = ImmutableDictionary.CreateBuilder<string, DestinationConfig>();
            foreach (var endpointSection in serviceSection.GetChildren())
            {
                foreach (var valueSection in endpointSection.GetChildren())
                {
                    if (string.IsNullOrWhiteSpace(valueSection.Value))
                    {
                        continue;
                    }

                    destinations[$"{serviceSection.Key}-{endpointSection.Key}-{valueSection.Key}"] =
                        new DestinationConfig
                        {
                            Address = valueSection.Value.EndsWith('/')
                                ? valueSection.Value
                                : valueSection.Value + "/",
                        };
                }
            }

            if (destinations.Count > 0)
            {
                result.Add(
                    new GatewayServiceEndpoint
                    {
                        ServiceName = serviceSection.Key,
                        Destinations = destinations.ToImmutable(),
                    }
                );
            }
        }

        if (result.Count == 0)
        {
            logger.LogWarning("Aspire 网关发现源未发现可用服务端点");
        }

        return result;
    }
}
