namespace Girvs.Refit.Discovery;

public sealed class AspireRefitServiceEndpointResolver : IRefitServiceEndpointResolver
{
    public bool CanResolve(RefitServiceAddressType addressType) =>
        addressType == RefitServiceAddressType.ServiceDiscovery;

    // 返回空表示不改写逻辑地址，让 AddServiceDiscovery 在 HttpClient 管道中解析。
    public Task<Uri> ResolveAsync(string serviceName, string? endpointName, CancellationToken cancellationToken) =>
        Task.FromResult<Uri>(null);
}
