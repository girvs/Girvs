using Girvs.Configuration;

namespace Girvs.Aspire.Gateway.Configuration;

public enum GatewayDiscoveryType
{
    Aspire,
    Kubernetes
}

public class GatewayDiscoveryConfig : IAppModuleConfig
{
    public GatewayDiscoveryType DiscoveryType { get; set; } = GatewayDiscoveryType.Aspire;

    public void Init() { }
}
