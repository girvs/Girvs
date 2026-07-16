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

    [Fact]
    public void 含连字符的同名服务也正确去重()
    {
        // 有意的行为改进：去重比较的是转换后的服务名（order_api == order_api），
        // 而旧网关 ConsulClientService 比较的是"转换后 x.ServiceName vs 未转换 Service"，
        // 对含连字符的重名服务去重会失效（"order_api" != "order-api"）。此测试固化正确行为。
        var agentServices = new Dictionary<string, AgentService>
        {
            ["id1"] = new AgentService { Service = "order-api", Address = "10.0.0.1", Port = 8080 },
            ["id2"] = new AgentService { Service = "order-api", Address = "10.0.0.2", Port = 8081 }
        };

        var ep = Assert.Single(ConsulGatewayServiceSource.MapAgentServices(agentServices));
        Assert.Equal("order_api", ep.ServiceName);
    }
}
