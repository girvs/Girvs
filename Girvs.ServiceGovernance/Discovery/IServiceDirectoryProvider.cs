namespace Girvs.ServiceGovernance.Discovery;

internal interface IServiceDirectoryProvider
{
    Task<IReadOnlyList<DiscoveredService>> GetServicesAsync(CancellationToken cancellationToken);
}
