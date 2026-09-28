using Girvs.ServiceGovernance.Configuration;

namespace Girvs.ServiceGovernance.Tests;

public class ServiceGovernanceConfigTests
{
    [Fact]
    public void 默认配置使用Aspire和WebApi模式()
    {
        var config = new ServiceGovernanceConfig();

        Assert.Equal(ServiceDiscoveryProvider.Aspire, config.ServiceDiscoveryProvider);
        Assert.Equal(ConsulServerModel.WebApi, config.CurrentServerModel);
        Assert.Equal("http://127.0.0.1:8500", config.ConsulAddress);
        Assert.Equal("http://127.0.0.1", config.ConsulRegistrationAddress);
        Assert.Equal("/health", config.HealthCheckPath);
        Assert.Equal("/alive", config.LivenessCheckPath);
    }

    [Fact]
    public void ModuleConfigurations节点绑定Consul模式()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string>
                {
                    ["ModuleConfigurations:ServiceGovernanceConfig:ServiceDiscoveryProvider"] =
                        "Consul",
                    ["ModuleConfigurations:ServiceGovernanceConfig:CurrentServerModel"] =
                        "GrpcService",
                    ["ModuleConfigurations:ServiceGovernanceConfig:ConsulAddress"] =
                        "http://consul:8500",
                    ["ModuleConfigurations:ServiceGovernanceConfig:ConsulRegistrationAddress"] =
                        "http://127.0.0.1:5080",
                    ["ModuleConfigurations:ServiceGovernanceConfig:HealthCheckPath"] = "/healthz",
                    ["ModuleConfigurations:ServiceGovernanceConfig:LivenessCheckPath"] =
                        "/alivez",
                }
            )
            .Build();
        var config = new ServiceGovernanceConfig();

        configuration.GetSection("ModuleConfigurations:ServiceGovernanceConfig").Bind(config);

        Assert.Equal(ServiceDiscoveryProvider.Consul, config.ServiceDiscoveryProvider);
        Assert.Equal(ConsulServerModel.GrpcService, config.CurrentServerModel);
        Assert.Equal("http://consul:8500", config.ConsulAddress);
        Assert.Equal("http://127.0.0.1:5080", config.ConsulRegistrationAddress);
        Assert.Equal("/healthz", config.HealthCheckPath);
        Assert.Equal("/alivez", config.LivenessCheckPath);
    }
}
