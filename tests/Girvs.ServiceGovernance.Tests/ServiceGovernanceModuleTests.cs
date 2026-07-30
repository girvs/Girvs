using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.ServiceDiscovery;
using Girvs.ServiceGovernance.Configuration;
using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Girvs.ServiceGovernance.Tests;

[Collection(OtlpEnvironmentVariableCollection.Name)]
public class ServiceGovernanceModuleTests
{
    private static IConfiguration BuildConfiguration(
        params (string Name, string Value)[] connectionStrings
    )
    {
        var data = connectionStrings.ToDictionary(
            x => $"ConnectionStrings:{x.Name}",
            x => x.Value
        );
        return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
    }

    private static AppSettings SetupSingletonAppSettings(params IConfig[] configs)
    {
        var appSettings = new AppSettings();
        appSettings.PreLoadModelConfig();
        if (configs.All(config => config is not ServiceGovernanceConfig))
        {
            appSettings.ModuleConfigurations.Add(
                nameof(ServiceGovernanceConfig),
                new ServiceGovernanceConfig()
            );
        }

        foreach (var config in configs)
        {
            appSettings.ModuleConfigurations.Add(config.GetType().Name, config);
        }

        Singleton<AppSettings>.Instance = appSettings;
        return appSettings;
    }

    [Fact]
    public void Provider为Aspire时注册服务发现()
    {
        SetupSingletonAppSettings(
            new ServiceGovernanceConfig
            {
                ServiceDiscoveryProvider = ServiceDiscoveryProvider.Aspire,
            }
        );
        var services = new ServiceCollection();

        new ServiceGovernanceModule().ConfigureServices(services, BuildConfiguration());

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IServiceEndpointProviderFactory)
        );
    }

    [Fact]
    public void ConfigureServices注册ServiceGovernanceConfig单例()
    {
        var config = new ServiceGovernanceConfig();
        SetupSingletonAppSettings(config);
        var services = new ServiceCollection();

        new ServiceGovernanceModule().ConfigureServices(services, BuildConfiguration());

        var provider = services.BuildServiceProvider();
        Assert.Same(config, provider.GetRequiredService<ServiceGovernanceConfig>());
    }

    [Fact]
    public void Provider为Consul时不注册Aspire服务发现()
    {
        SetupSingletonAppSettings(
            new ServiceGovernanceConfig
            {
                ServiceDiscoveryProvider = ServiceDiscoveryProvider.Consul,
            }
        );
        var services = new ServiceCollection();

        new ServiceGovernanceModule().ConfigureServices(services, BuildConfiguration());

        Assert.DoesNotContain(
            services,
            descriptor => descriptor.ServiceType == typeof(IServiceEndpointProviderFactory)
        );
    }

    [Fact]
    public void Provider为Consul且WebApi模式时注册Consul注册器但不注册IConsulClient()
    {
        SetupSingletonAppSettings(
            new ServiceGovernanceConfig
            {
                ServiceDiscoveryProvider = ServiceDiscoveryProvider.Consul,
                CurrentServerModel = ConsulServerModel.WebApi,
            }
        );
        var services = new ServiceCollection();

        new ServiceGovernanceModule().ConfigureServices(services, BuildConfiguration());

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IConsulServiceRegistrar)
        );
        Assert.DoesNotContain(
            services,
            descriptor => descriptor.ServiceType == typeof(IConsulClient)
        );
    }

    [Fact]
    public void Provider为Consul且GrpcService模式时注册IConsulClient单例()
    {
        SetupSingletonAppSettings(
            new ServiceGovernanceConfig
            {
                ServiceDiscoveryProvider = ServiceDiscoveryProvider.Consul,
                CurrentServerModel = ConsulServerModel.GrpcService,
            }
        );
        var services = new ServiceCollection();

        new ServiceGovernanceModule().ConfigureServices(services, BuildConfiguration());

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(IConsulClient)
                && descriptor.Lifetime == ServiceLifetime.Singleton
        );
    }

    [Fact]
    public async Task ConfigureMapEndpointRoute映射health和alive端点()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddHealthChecks();
        await using var application = builder.Build();

        new ServiceGovernanceModule().ConfigureMapEndpointRoute(application);

        var patterns = ((IEndpointRouteBuilder)application)
            .DataSources.SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(endpoint => endpoint.RoutePattern.RawText)
            .ToArray();
        Assert.Contains("/health", patterns);
        Assert.Contains("/alive", patterns);
    }

    [Fact]
    public void 模块Order为极小值确保先于其他模块执行()
    {
        Assert.True(new ServiceGovernanceModule().Order < 0);
    }

    [Fact]
    public void ConfigureServices注册健康检查服务()
    {
        SetupSingletonAppSettings();
        var services = new ServiceCollection();
        var configuration = BuildConfiguration();

        new ServiceGovernanceModule().ConfigureServices(services, configuration);

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(HealthCheckService)
        );
    }

    [Fact]
    public void 未设置OTLP端点时不注册OpenTelemetry()
    {
        var original = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        try
        {
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", null);
            SetupSingletonAppSettings();
            var services = new ServiceCollection();

            new ServiceGovernanceModule().ConfigureServices(services, BuildConfiguration());

            Assert.DoesNotContain(
                services,
                descriptor =>
                    descriptor.ImplementationType?.FullName?.Contains(
                        "OpenTelemetry",
                        StringComparison.Ordinal
                    ) == true
            );
        }
        finally
        {
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", original);
        }
    }
}
