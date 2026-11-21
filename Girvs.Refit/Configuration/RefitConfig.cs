namespace Girvs.Refit.Configuration;

#if NET10_0_OR_GREATER
[Obsolete("net10.0 下请使用 ServiceGovernanceConfig.ServiceDiscoveryProvider")]
#endif
public enum RefitDiscoveryProvider
{
    // 兼容历史 Consul 注册与健康实例查询。
    Consul,

    // 交由 Girvs.ServiceGovernance 配置的 .NET 服务发现解析逻辑服务地址。
    Aspire
}

public class RefitConfig : IAppModuleConfig
{
    // 只影响 ServiceDiscovery 类型接口；Static 类型始终读取固定地址。
#if NET10_0_OR_GREATER
    [Obsolete("net10.0 下此属性不再生效，由 ServiceGovernanceConfig.ServiceDiscoveryProvider 统一控制")]
#endif
    public RefitDiscoveryProvider DiscoveryProvider { get; set; } = RefitDiscoveryProvider.Aspire;

    public string ConsulAddress { get; set; }

    // 兼容历史配置：经私有字段读取，供 GetConsulAddress 回退使用；属性移除时一并删除。
    private string _consulServiceHost;

    [Obsolete("请使用 ConsulAddress")]
    public string ConsulServiceHost
    {
        get => _consulServiceHost;
        set => _consulServiceHost = value;
    }

    public Dictionary<string, string> ServiceEndpoints { get; set; } = new();

    // 兼容历史配置：经私有字段读取，供 GetServiceEndpoint 回退使用；属性移除时一并删除。
    private Dictionary<string, string> _serviceAddress = new();

    [Obsolete("请使用 ServiceEndpoints")]
    public Dictionary<string, string> ServiceAddress
    {
        get => _serviceAddress;
        set => _serviceAddress = value;
    }

    [Obsolete("请使用 GetServiceEndpoint")]
    public string this[string index] => GetServiceEndpoint(index);

    // 新配置优先，随后回退到历史 ServiceAddress，方便应用逐步迁移。
    public string GetServiceEndpoint(string serviceName)
    {
        if (ServiceEndpoints.TryGetValue(serviceName, out var endpoint))
            return endpoint;

        return _serviceAddress.TryGetValue(serviceName, out endpoint) ? endpoint : null;
    }

    // 兼容旧 ConsulServiceHost，未配置时采用本地 Consul 默认地址。
    public string GetConsulAddress() =>
        !string.IsNullOrWhiteSpace(ConsulAddress)
            ? ConsulAddress
            : !string.IsNullOrWhiteSpace(_consulServiceHost)
                ? _consulServiceHost
                : "http://127.0.0.1:8500";

    public void Init() { }
}
