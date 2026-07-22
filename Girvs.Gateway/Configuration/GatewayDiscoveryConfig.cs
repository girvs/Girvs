using Girvs.Configuration;

namespace Girvs.Gateway.Configuration;

public enum GatewayDiscoveryType
{
    Aspire,
    Consul,
    Kubernetes
}

public class GatewayDiscoveryConfig : IAppModuleConfig
{
    public GatewayDiscoveryType DiscoveryType { get; set; } = GatewayDiscoveryType.Aspire;
    public string ConsulAddress { get; set; } = "http://192.168.51.166:8500";

    public void Init() { }
}
