namespace Girvs.Refit.Configuration;

public enum RefitDiscoveryProvider
{
    Consul,
    Aspire
}

public class RefitConfig : IAppModuleConfig
{
    public RefitDiscoveryProvider DiscoveryProvider { get; set; } = RefitDiscoveryProvider.Aspire;

    public string ConsulAddress { get; set; }

    [Obsolete("请使用 ConsulAddress")]
    public string ConsulServiceHost { get; set; }

    public Dictionary<string, string> ServiceEndpoints { get; set; } = new();

    [Obsolete("请使用 ServiceEndpoints")]
    public Dictionary<string, string> ServiceAddress { get; set; } = new();

    [Obsolete("请使用 GetServiceEndpoint")]
    public string this[string index] => GetServiceEndpoint(index);

    public string GetServiceEndpoint(string serviceName)
    {
        if (ServiceEndpoints.TryGetValue(serviceName, out var endpoint))
            return endpoint;

        return ServiceAddress.TryGetValue(serviceName, out endpoint) ? endpoint : null;
    }

    public string GetConsulAddress() =>
        !string.IsNullOrWhiteSpace(ConsulAddress)
            ? ConsulAddress
            : !string.IsNullOrWhiteSpace(ConsulServiceHost)
                ? ConsulServiceHost
                : "http://127.0.0.1:8500";

    public void Init() { }
}
