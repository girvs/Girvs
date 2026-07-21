using Girvs.Aspire.Gateway.Discovery;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Girvs.Aspire.Gateway.Tests;

public class AspireGatewayServiceSourceTests
{
    [Fact]
    public void 读取Aspire注入端点_生成服务快照()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["services:service-a:http:0"] = "http://127.0.0.1:5101",
                    ["services:service-b:http:0"] = "https://127.0.0.1:7102",
                }
            )
            .Build();

        var endpoints = AspireGatewayServiceSource.MapConfiguration(
            configuration,
            NullLogger.Instance
        );

        Assert.Collection(
            endpoints.OrderBy(x => x.ServiceName),
            service =>
            {
                Assert.Equal("service-a", service.ServiceName);
                Assert.Equal(
                    "http://127.0.0.1:5101/",
                    Assert.Single(service.Destinations).Value.Address
                );
            },
            service =>
            {
                Assert.Equal("service-b", service.ServiceName);
                Assert.Equal(
                    "https://127.0.0.1:7102/",
                    Assert.Single(service.Destinations).Value.Address
                );
            }
        );
    }

    [Fact]
    public async Task StartAsync_构建快照并触发变更事件()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["services:service-a:http:0"] = "http://127.0.0.1:5101",
                }
            )
            .Build();
        var source = new AspireGatewayServiceSource(
            configuration,
            NullLogger<AspireGatewayServiceSource>.Instance
        );
        var changed = false;
        source.ServicesChanged += () => changed = true;

        await source.StartAsync(CancellationToken.None);

        Assert.True(changed);
        Assert.Single(source.GetServices());
    }
}
