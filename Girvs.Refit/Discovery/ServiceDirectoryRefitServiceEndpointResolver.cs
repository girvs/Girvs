#if NET10_0
namespace Girvs.Refit.Discovery;

/// <summary>基于统一服务目录解析 Refit 的内部服务地址。</summary>
public sealed class ServiceDirectoryRefitServiceEndpointResolver(IServiceDirectory directory)
    : IRefitServiceEndpointResolver
{
    public bool CanResolve(RefitServiceAddressType addressType) =>
        addressType == RefitServiceAddressType.ServiceDiscovery;

    public Task<Uri> ResolveAsync(string serviceName, string? endpointName, CancellationToken cancellationToken)
    {
        var service = directory.GetService(serviceName)
            ?? throw new GirvsException($"Refit 服务 {serviceName} 在服务目录中不存在可路由实例");
        var endpoints = service.Endpoints.Where(endpoint =>
            string.IsNullOrWhiteSpace(endpointName)
                ? endpoint.Address.Scheme is "http" or "https"
                : string.Equals(endpoint.EndpointName, endpointName, StringComparison.OrdinalIgnoreCase)
        ).ToArray();
        if (endpoints.Length == 0)
            throw new GirvsException($"Refit 服务 {serviceName} 未找到端点 {endpointName}");

        if (string.IsNullOrWhiteSpace(endpointName))
        {
            var names = endpoints.Select(endpoint => endpoint.EndpointName).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (names.Length != 1)
                throw new GirvsException($"Refit 服务 {serviceName} 存在多个 HTTP(S) 端点，请指定 EndpointName");
        }

        return Task.FromResult(endpoints[Random.Shared.Next(endpoints.Length)].Address);
    }
}
#endif
