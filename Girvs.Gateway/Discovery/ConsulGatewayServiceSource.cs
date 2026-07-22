using Consul;
using DestinationConfig = Yarp.ReverseProxy.Configuration.DestinationConfig;

namespace Girvs.Gateway.Discovery;

/// <summary>基于 Consul Agent 服务目录的网关服务发现源：30 秒轮询，适用于非 K8s 部署。</summary>
public sealed class ConsulGatewayServiceSource(IConsulClient consulClient)
    : IGatewayServiceDiscoverySource, IDisposable
{
    private volatile IReadOnlyList<GatewayServiceEndpoint> _services = new List<GatewayServiceEndpoint>();
    private Timer? _timer;

    public event Action? ServicesChanged;

    public IReadOnlyList<GatewayServiceEndpoint> GetServices() => _services;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(_ => Refresh(), null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
        return Task.CompletedTask;
    }

    private void Refresh()
    {
        try
        {
            var agentServices = consulClient.Agent.Services().Result.Response;
            _services = MapAgentServices(agentServices);
            ServicesChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Girvs.Gateway] Consul 刷新失败：{ex.Message}");
        }
    }

    internal static List<GatewayServiceEndpoint> MapAgentServices(
        IDictionary<string, AgentService> agentServices
    )
    {
        var result = new List<GatewayServiceEndpoint>();
        foreach (var agentService in agentServices)
        {
            var serviceName = agentService.Value.Service.Replace("-", "_");
            if (result.Exists(x => x.ServiceName == serviceName))
            {
                continue;
            }

            var address = agentService.Value.Address;
            var port = agentService.Value.Port;
            result.Add(
                new GatewayServiceEndpoint
                {
                    ServiceName = serviceName,
                    Destinations = new Dictionary<string, DestinationConfig>
                    {
                        [$"{serviceName}-{port}"] = new DestinationConfig
                        {
                            Address = $"http://{address}:{port}"
                        }
                    }
                }
            );
        }

        return result;
    }

    public void Dispose() => _timer?.Dispose();
}
