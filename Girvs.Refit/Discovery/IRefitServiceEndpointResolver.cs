namespace Girvs.Refit.Discovery;

public interface IRefitServiceEndpointResolver
{
    bool CanResolve(RefitServiceAddressType addressType);

    Task<Uri> ResolveAsync(string serviceName, CancellationToken cancellationToken);
}
