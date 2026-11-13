namespace Girvs.ServiceGovernance.Configuration;

/// <summary>
/// 服务发现提供程序。
/// </summary>
public enum ServiceDiscoveryProvider
{
    Aspire,
    Consul,
    Kubernetes,
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

    /// <summary>目录刷新间隔（秒）。</summary>
    public int DiscoveryRefreshInterval { get; set; } = 30;

    /// <summary>最后一次成功刷新后的最大可接受陈旧时间（秒）。</summary>
    public int MaxStaleDuration { get; set; } = 90;

    /// <summary>Kubernetes 服务发现的命名空间；为空时查询全部命名空间。</summary>
    public string? KubernetesNamespace { get; set; }

    /// <summary>用于筛选业务 Kubernetes Service 的 Label Selector。</summary>
    public string KubernetesLabelSelector { get; set; } = "girvs.io/business=true";

    /// <summary>Aspire 注入端点的服务元数据，未配置服务默认不通过网关公开。</summary>
    public Dictionary<string, ServiceGovernanceServiceConfig> Services { get; set; } = new();

    /// <summary>服务对外注册基址（仅 Consul 模式生效），如 http://127.0.0.1:8080，不含路径；为空时告警并跳过注册。</summary>
    public string? ConsulRegistrationAddress { get; set; } = "http://127.0.0.1";

    /// <summary>完整健康验证路径（readiness/部署后验证），默认 /health；Consul HTTP 检查、Aspire Readiness probe、K8s readinessProbe、框架端点映射共用。</summary>
    public string HealthCheckPath { get; set; } = "/health";

    /// <summary>存活探测路径（liveness），默认 /alive；Aspire Liveness probe、K8s livenessProbe、框架端点映射使用；Consul 模式忽略。</summary>
    public string LivenessCheckPath { get; set; } = "/alive";

    public string ServerName { get; set; } = "";

    public int Interval { get; set; } = 10;

    public int DeregisterCriticalServiceAfter { get; set; } = 90;

    public int Timeout { get; set; } = 30;

    public ConsulServerModel CurrentServerModel { get; set; } = ConsulServerModel.WebApi;

    public void Init() { }
}

/// <summary>服务目录中的业务服务元数据。</summary>
public class ServiceGovernanceServiceConfig
{
    public bool GatewayEnabled { get; set; }

    public string? GatewayEndpointName { get; set; }
}
