namespace Girvs.Refit.Configuration;

public enum RefitDiscoveryProvider
{
    // 兼容历史 Consul 注册与健康实例查询。
    Consul,

    // 交由 Girvs.Aspire 配置的 .NET 服务发现解析逻辑服务地址。
    Aspire
}

public class RefitConfig : IAppModuleConfig
{
    // 只影响 ServiceDiscovery 类型接口；Static 类型始终读取固定地址。
    public RefitDiscoveryProvider DiscoveryProvider { get; set; } = RefitDiscoveryProvider.Aspire;

    public string ConsulAddress { get; set; }

    [Obsolete("请使用 ConsulAddress")]
    public string ConsulServiceHost { get; set; }

    public Dictionary<string, string> ServiceEndpoints { get; set; } = new();

    [Obsolete("请使用 ServiceEndpoints")]
    public Dictionary<string, string> ServiceAddress { get; set; } = new();

    [Obsolete("请使用 GetServiceEndpoint")]
    public string this[string index] => GetServiceEndpoint(index);

    // 新配置优先，随后回退到历史 ServiceAddress，方便应用逐步迁移。
    public string GetServiceEndpoint(string serviceName)
    {
        if (ServiceEndpoints.TryGetValue(serviceName, out var endpoint))
            return endpoint;

        return ServiceAddress.TryGetValue(serviceName, out endpoint) ? endpoint : null;
    }

    // 兼容旧 ConsulServiceHost，未配置时采用本地 Consul 默认地址。
    public string GetConsulAddress() =>
        !string.IsNullOrWhiteSpace(ConsulAddress)
            ? ConsulAddress
            : !string.IsNullOrWhiteSpace(ConsulServiceHost)
                ? ConsulServiceHost
                : "http://127.0.0.1:8500";

    public void Init() { }
}
