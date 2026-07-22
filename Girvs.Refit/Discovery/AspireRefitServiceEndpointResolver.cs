namespace Girvs.Refit.Discovery;

public sealed class AspireRefitServiceEndpointResolver : IRefitServiceEndpointResolver
{
    public bool CanResolve(RefitServiceAddressType addressType) =>
        addressType == RefitServiceAddressType.ServiceDiscovery;

    public Task<Uri> ResolveAsync(string serviceName, CancellationToken cancellationToken) =>
        Task.FromResult<Uri>(null);
}
