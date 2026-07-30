using Consul;
using Girvs.ServiceGovernance.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

namespace Girvs.ServiceGovernance.Tests;

public class ConsulServiceRegistrarTests
{
    [Fact]
    public void WebApi注册信息使用统一health端点()
    {
        var config = CreateConfig(ConsulServerModel.WebApi);

        var registration = ConsulServiceRegistrar.CreateWebApiRegistration(config);

        Assert.Equal("sample-service", registration.Name);
        Assert.Equal("127.0.0.1", registration.Address);
        Assert.Equal(5080, registration.Port);
        Assert.Equal("http://127.0.0.1:5080/health", registration.Check.HTTP);
        Assert.Null(registration.Check.GRPC);
        Assert.Equal(TimeSpan.FromSeconds(10), registration.Check.Interval);
        Assert.Equal(TimeSpan.FromSeconds(30), registration.Check.Timeout);
        Assert.Equal(TimeSpan.FromSeconds(90), registration.Check.DeregisterCriticalServiceAfter);
    }

    [Fact]
    public void Grpc注册信息使用标准GrpcHealth地址()
    {
        var config = CreateConfig(ConsulServerModel.GrpcService);

        var registration = ConsulServiceRegistrar.CreateGrpcRegistration(config);

        Assert.Equal("sample-service", registration.Name);
        Assert.Equal("127.0.0.1", registration.Address);
        Assert.Equal(5080, registration.Port);
        Assert.Equal("127.0.0.1:5080/health", registration.Check.GRPC);
        Assert.Null(registration.Check.HTTP);
    }

    [Fact]
    public void Consul注册器抛异常时Configure软失败()
    {
        var config = CreateConfig(ConsulServerModel.WebApi);
        SetupSingletonAppSettings(config);
        var registrar = new ThrowingConsulServiceRegistrar();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConsulServiceRegistrar>(registrar);
        var application = new ApplicationBuilder(services.BuildServiceProvider());

        var exception = Record.Exception(
            () => new ServiceGovernanceModule().Configure(application, null)
        );

        Assert.Null(exception);
        Assert.Equal(1, registrar.RegisterCount);
    }

    [Fact]
    public void ConsulAddress为空时跳过注册()
    {
        var config = CreateConfig(ConsulServerModel.WebApi);
        config.ConsulAddress = "";
        SetupSingletonAppSettings(config);
        var registrar = new CountingConsulServiceRegistrar();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConsulServiceRegistrar>(registrar);
        var application = new ApplicationBuilder(services.BuildServiceProvider());

        var exception = Record.Exception(
            () => new ServiceGovernanceModule().Configure(application, null)
        );

        Assert.Null(exception);
        Assert.Equal(0, registrar.RegisterCount);
    }

    private static ServiceGovernanceConfig CreateConfig(ConsulServerModel serverModel) =>
        new()
        {
            ServiceDiscoveryProvider = ServiceDiscoveryProvider.Consul,
            CurrentServerModel = serverModel,
            ServerName = "sample-service",
            ConsulAddress = "http://127.0.0.1:8500",
            HealthAddress = "http://127.0.0.1:5080/health",
            Interval = 10,
            Timeout = 30,
            DeregisterCriticalServiceAfter = 90,
        };

    private static void SetupSingletonAppSettings(ServiceGovernanceConfig config)
    {
        var appSettings = new AppSettings();
        appSettings.PreLoadModelConfig();
        appSettings.ModuleConfigurations.Add(nameof(ServiceGovernanceConfig), config);
        Singleton<AppSettings>.Instance = appSettings;
    }

    private sealed class ThrowingConsulServiceRegistrar : IConsulServiceRegistrar
    {
        public int RegisterCount { get; private set; }

        public void Register(ServiceGovernanceConfig config)
        {
            RegisterCount++;
            throw new InvalidOperationException("Consul 不可用");
        }
    }

    private sealed class CountingConsulServiceRegistrar : IConsulServiceRegistrar
    {
        public int RegisterCount { get; private set; }

        public void Register(ServiceGovernanceConfig config) => RegisterCount++;
    }
}
