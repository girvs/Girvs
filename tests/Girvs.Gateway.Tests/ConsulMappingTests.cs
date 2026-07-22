using Consul;
using Girvs.Aspire.Gateway.Discovery;

namespace Girvs.Aspire.Gateway.Tests;

public class ConsulMappingTests
{
    [Fact]
    public void Consul服务映射为端点_服务名连字符转下划线_地址拼接()
    {
        var endpoints = ConsulGatewayServiceSource.MapAgentServices(
            new Dictionary<string, AgentService>
            {
                ["service-a-1"] = new()
                {
                    Service = "service-a",
                    Address = "10.0.0.11",
                    Port = 5001
                },
                ["service-b-1"] = new()
                {
                    Service = "service-b",
                    Address = "10.0.0.12",
                    Port = 5002
                }
            }
        );

        Assert.Collection(
            endpoints,
            endpoint =>
            {
                Assert.Equal("service_a", endpoint.ServiceName);
                var destination = Assert.Single(endpoint.Destinations);
                Assert.Equal("service_a-5001", destination.Key);
                Assert.Equal("http://10.0.0.11:5001", destination.Value.Address);
            },
            endpoint =>
            {
                Assert.Equal("service_b", endpoint.ServiceName);
                var destination = Assert.Single(endpoint.Destinations);
                Assert.Equal("service_b-5002", destination.Key);
                Assert.Equal("http://10.0.0.12:5002", destination.Value.Address);
            }
        );
    }

    [Fact]
    public void Consul服务映射为端点_重复服务名只保留首个实例()
    {
        var endpoints = ConsulGatewayServiceSource.MapAgentServices(
            new Dictionary<string, AgentService>
            {
                ["service-a-1"] = new()
                {
                    Service = "service-a",
                    Address = "10.0.0.11",
                    Port = 5001
                },
                ["service-a-2"] = new()
                {
                    Service = "service-a",
                    Address = "10.0.0.12",
                    Port = 5002
                }
            }
        );

        var endpoint = Assert.Single(endpoints);
        Assert.Equal("service_a", endpoint.ServiceName);
        var destination = Assert.Single(endpoint.Destinations);
        Assert.Equal("service_a-5001", destination.Key);
        Assert.Equal("http://10.0.0.11:5001", destination.Value.Address);
    }
}
