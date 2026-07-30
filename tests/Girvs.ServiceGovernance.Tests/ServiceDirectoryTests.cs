using Girvs.ServiceGovernance.Discovery;

namespace Girvs.ServiceGovernance.Tests;

public class ServiceDirectoryTests
{
    [Fact]
    public void 发布新快照后_可按服务名读取服务并通知变更()
    {
        var directory = new ServiceDirectory();
        var changed = 0;
        directory.ServicesChanged += () => changed++;

        directory.Publish(
            [
                new DiscoveredService
                {
                    ServiceName = "orders",
                    Endpoints =
                    [
                        new DiscoveredServiceEndpoint
                        {
                            EndpointName = "http",
                            Address = new Uri("http://orders:8080"),
                        },
                    ],
                },
            ]
        );

        var service = directory.GetService("orders");
        Assert.NotNull(service);
        Assert.Equal("http", Assert.Single(service.Endpoints).EndpointName);
        Assert.Equal(1, changed);
    }

    [Fact]
    public void 发布内容相同的快照时_不重复通知变更()
    {
        var directory = new ServiceDirectory();
        var services = new[]
        {
            new DiscoveredService
            {
                ServiceName = "orders",
                Endpoints =
                [
                    new DiscoveredServiceEndpoint
                    {
                        EndpointName = "http",
                        Address = new Uri("http://orders:8080"),
                    },
                ],
            },
        };
        var changed = 0;
        directory.ServicesChanged += () => changed++;

        directory.Publish(services);
        directory.Publish(services);

        Assert.Equal(1, changed);
    }
}
