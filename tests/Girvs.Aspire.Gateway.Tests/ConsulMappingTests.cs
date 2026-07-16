using System.Collections.Generic;
using Consul;
using Girvs.Aspire.Gateway.Discovery;

namespace Girvs.Aspire.Gateway.Tests;

public class ConsulMappingTests
{
    [Fact]
    public void Consul服务映射为端点_服务名连字符转下划线_地址拼接()
    {
        var agentServices = new Dictionary<string, AgentService>
        {
            ["id1"] = new AgentService { Service = "order-api", Address = "10.0.0.1", Port = 8080 }
        };

        var endpoints = ConsulGatewayServiceSource.MapAgentServices(agentServices);

        var ep = Assert.Single(endpoints);
        Assert.Equal("order_api", ep.ServiceName);
        var dest = Assert.Single(ep.Destinations);
        Assert.Equal("order_api-8080", dest.Key);
        Assert.Equal("http://10.0.0.1:8080", dest.Value.Address);
    }

    [Fact]
    public void 同名服务多实例去重()
    {
        var agentServices = new Dictionary<string, AgentService>
        {
            ["id1"] = new AgentService { Service = "a", Address = "10.0.0.1", Port = 80 },
            ["id2"] = new AgentService { Service = "a", Address = "10.0.0.2", Port = 80 }
        };
        Assert.Single(ConsulGatewayServiceSource.MapAgentServices(agentServices));
    }
}
