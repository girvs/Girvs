using Girvs.ServiceGovernance.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.Refit.Tests;

public class RefitServiceGovernanceIntegrationTests
{
    [Fact]
    public void net10注册统一服务目录解析器()
    {
        var services = new ServiceCollection();

        RefitModule.RegisterEndpointResolvers(
            services,
            new RefitConfig()
        );

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(IRefitServiceEndpointResolver)
                && descriptor.ImplementationType == typeof(ServiceDirectoryRefitServiceEndpointResolver)
        );
        Assert.DoesNotContain(
            services,
            descriptor => descriptor.ImplementationType == typeof(ConsulRefitServiceEndpointResolver)
        );
    }

}
