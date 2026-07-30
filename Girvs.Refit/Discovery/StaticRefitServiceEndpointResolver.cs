namespace Girvs.Refit.Discovery;

public sealed class StaticRefitServiceEndpointResolver(RefitConfig config)
    : IRefitServiceEndpointResolver
{
    public bool CanResolve(RefitServiceAddressType addressType) =>
        addressType == RefitServiceAddressType.Static;

    public Task<Uri> ResolveAsync(string serviceName, string? endpointName, CancellationToken cancellationToken)
    {
        // 固定地址仅允许来自配置，不能误回退为内部服务发现。
        var endpoint = config.GetServiceEndpoint(serviceName);
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
            throw new GirvsException($"Refit 服务 {serviceName} 未配置静态请求地址");

        return Task.FromResult(uri);
    }
}
