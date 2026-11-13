using Microsoft.Extensions.DependencyInjection;

namespace Girvs.Gateway.Tests;

public class GirvsGatewayExtensionsTests
{
    [Fact]
    public void 注册Yarp配置提供者()
    {
        var services = new ServiceCollection();
        services.AddGirvsGateway();

        var descriptors = services.ToList();
        Assert.Contains(
            descriptors,
            d => d.ServiceType == typeof(Yarp.ReverseProxy.Configuration.IProxyConfigProvider)
        );
    }
}
