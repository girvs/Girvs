using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Girvs.Aspire.Gateway.Discovery;

/// <summary>基于 Aspire AppHost 注入配置的本地网关服务发现源。</summary>
public sealed class AspireGatewayServiceSource : IGatewayServiceDiscoverySource
{
    public AspireGatewayServiceSource(
        IConfiguration configuration,
        ILogger<AspireGatewayServiceSource> logger
    ) { }

    public event Action? ServicesChanged;

    public IReadOnlyList<GatewayServiceEndpoint> GetServices() => [];

    public Task StartAsync(CancellationToken cancellationToken)
    {
        ServicesChanged?.Invoke();
        return Task.CompletedTask;
    }
}
