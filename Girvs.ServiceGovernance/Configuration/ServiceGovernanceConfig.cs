namespace Girvs.ServiceGovernance.Configuration;

/// <summary>
/// 服务发现提供程序。
/// </summary>
public enum ServiceDiscoveryProvider
{
    Aspire,
    Consul,
}

/// <summary>
/// Consul 服务注册模式。
/// </summary>
public enum ConsulServerModel
{
    WebApi,
    GrpcService,
}

/// <summary>
/// 服务治理配置。
/// </summary>
public class ServiceGovernanceConfig : IAppModuleConfig
{
    public ServiceDiscoveryProvider ServiceDiscoveryProvider { get; set; } =
        ServiceDiscoveryProvider.Aspire;

    public string ConsulAddress { get; set; } = "http://127.0.0.1:8500";

    public string HealthAddress { get; set; } = "http://127.0.0.1/health";

    public string ServerName { get; set; } = "";

    public int Interval { get; set; } = 10;

    public int DeregisterCriticalServiceAfter { get; set; } = 90;

    public int Timeout { get; set; } = 30;

    public ConsulServerModel CurrentServerModel { get; set; } = ConsulServerModel.WebApi;

    public void Init() { }
}
