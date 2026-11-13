using Girvs.ServiceGovernance.Configuration;
using Girvs.ServiceGovernance.Discovery;
using Microsoft.Extensions.Logging.Abstractions;

namespace Girvs.ServiceGovernance.Tests;

public class AspireServiceDirectoryProviderTests
{
    [Fact]
    public void Aspire注入端点映射为业务服务并应用网关元数据()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["services:orders:http:0"] = "http://127.0.0.1:5101",
                    ["services:orders:https:0"] = "https://127.0.0.1:7101",
                }
            )
            .Build();
        var config = new ServiceGovernanceConfig
        {
            Services = new Dictionary<string, ServiceGovernanceServiceConfig>
            {
                ["orders"] = new()
                {
                    GatewayEnabled = true,
                    GatewayEndpointName = "https",
                },
            },
        };

        var services = AspireServiceDirectoryProvider.MapConfiguration(
            configuration,
            config,
            NullLogger.Instance
        );

        var service = Assert.Single(services);
        Assert.True(service.GatewayEnabled);
        Assert.Equal("https", service.GatewayEndpointName);
        Assert.Equal(2, service.Endpoints.Count);
        Assert.Equal("http", service.Endpoints[0].EndpointName);
    }
}
