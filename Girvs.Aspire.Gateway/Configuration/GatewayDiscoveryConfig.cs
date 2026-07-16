using Girvs.Configuration;

namespace Girvs.Aspire.Gateway.Configuration;

public enum GatewayDiscoveryType
{
    Consul,
    Kubernetes
}

public class GatewayDiscoveryConfig : IAppModuleConfig
{
    public GatewayDiscoveryType DiscoveryType { get; set; } = GatewayDiscoveryType.Consul;
    public string ConsulAddress { get; set; } = "http://192.168.51.166:8500";

    public void Init() { }
}
