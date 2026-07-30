using Girvs.ServiceGovernance.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.Refit.Tests;

public class RefitServiceGovernanceIntegrationTests
{
    [Fact]
    public void Provider为Aspire时注册Aspire解析器()
    {
        var services = new ServiceCollection();

        RefitModule.RegisterEndpointResolvers(
            services,
            new RefitConfig(),
            new ServiceGovernanceConfig
            {
                ServiceDiscoveryProvider = ServiceDiscoveryProvider.Aspire,
            }
        );

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(IRefitServiceEndpointResolver)
                && descriptor.ImplementationType == typeof(AspireRefitServiceEndpointResolver)
        );
        Assert.DoesNotContain(
            services,
            descriptor => descriptor.ImplementationType == typeof(ConsulRefitServiceEndpointResolver)
        );
    }

    [Fact]
    public void Provider为Consul时注册Consul解析器()
    {
        var services = new ServiceCollection();

        RefitModule.RegisterEndpointResolvers(
            services,
            new RefitConfig(),
            new ServiceGovernanceConfig
            {
                ServiceDiscoveryProvider = ServiceDiscoveryProvider.Consul,
            }
        );

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(IRefitServiceEndpointResolver)
                && descriptor.ImplementationType == typeof(ConsulRefitServiceEndpointResolver)
        );
        Assert.DoesNotContain(
            services,
            descriptor => descriptor.ImplementationType == typeof(AspireRefitServiceEndpointResolver)
        );
    }
}
