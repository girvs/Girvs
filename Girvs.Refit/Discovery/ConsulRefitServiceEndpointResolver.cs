namespace Girvs.Refit.Discovery;

public sealed class ConsulRefitServiceEndpointResolver(RefitConfig config)
    : IRefitServiceEndpointResolver
{
    public bool CanResolve(RefitServiceAddressType addressType) =>
        addressType == RefitServiceAddressType.ServiceDiscovery;

    public async Task<Uri> ResolveAsync(string serviceName, CancellationToken cancellationToken)
    {
        // Consul 模式在每次请求时选择健康实例，实例变更无需重启调用方。
        using var client = new ConsulClient(options => options.Address = new Uri(config.GetConsulAddress()));
        var response = await client.Health.Service(serviceName, string.Empty, true);
        var entries = response.Response;
        if (entries is null || entries.Length == 0)
            throw new GirvsException($"Refit 服务 {serviceName} 在 Consul 中不存在健康实例");

        // 在健康实例间随机分配请求，保持旧实现的负载均衡行为。
        var entry = entries[Random.Shared.Next(entries.Length)];
        var address = string.IsNullOrWhiteSpace(entry.Service.Address)
            ? entry.Node.Address
            : entry.Service.Address;
        return new Uri($"http://{address}:{entry.Service.Port}");
    }
}
