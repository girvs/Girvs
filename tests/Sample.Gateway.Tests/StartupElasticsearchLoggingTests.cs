using Girvs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sample.Gateway;

namespace Sample.Gateway.Tests;

public class StartupElasticsearchLoggingTests
{
    [Fact]
    public void ConfigureServices_配置Elasticsearch地址时_不注册ElasticsearchSink()
    {
        GirvsSerilogSinkRegistry.SnapshotAndClear();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Resources:elastic-search:Settings:NodeUrls"] = "http://localhost:9200",
                }
            )
            .Build();

        var startup = new Startup(configuration, null!);
        startup.ConfigureServices(new ServiceCollection());

        var registrations = GirvsSerilogSinkRegistry.SnapshotAndClear();
        Assert.DoesNotContain(registrations, registration => registration.OverriddenSinkTypeNames.Contains("Elasticsearch"));
    }
}
