using Microsoft.Extensions.ServiceDiscovery;
using Girvs.ServiceGovernance.Configuration;
using Consul;
using Girvs.ServiceGovernance.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Threading;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using HealthCheckService = Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService;

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
        SetupSingletonAppSettings();
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
    public async Task ConfigureMapEndpointRoute使用自定义HealthCheckPath()
    {
        SetupSingletonAppSettings(
            new ServiceGovernanceConfig { HealthCheckPath = "/healthz", LivenessCheckPath = "/alivez" }
        );
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddHealthChecks();
        await using var application = builder.Build();

        new ServiceGovernanceModule().ConfigureMapEndpointRoute(application);

        var patterns = ((IEndpointRouteBuilder)application)
            .DataSources.SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(endpoint => endpoint.RoutePattern.RawText)
            .ToArray();
        Assert.Contains("/healthz", patterns);
        Assert.Contains("/alivez", patterns);
        Assert.DoesNotContain("/health", patterns);
    }

    [Fact]
    public async Task ConfigureMapEndpointRoute_liveness仅执行live标签检查()
    {
        SetupSingletonAppSettings();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseSetting("urls", "http://127.0.0.1:0");
        var liveExecutions = 0;
        var otherExecutions = 0;
        builder
            .Services.AddHealthChecks()
            .AddCheck(
                "live-ok",
                () =>
                {
                    Interlocked.Increment(ref liveExecutions);
                    return HealthCheckResult.Healthy();
                },
                tags: ["live"]
            )
            .AddCheck(
                "other-fail",
                () =>
                {
                    Interlocked.Increment(ref otherExecutions);
                    return HealthCheckResult.Unhealthy();
                },
                tags: ["other"]
            );
        await using var application = builder.Build();

        new ServiceGovernanceModule().ConfigureMapEndpointRoute(application);
        await application.StartAsync();
        using var client = new HttpClient
        {
            BaseAddress = new Uri(application.Urls.First()),
        };

        var livenessResponse = await client.GetAsync("/alive");

        Assert.Equal(1, liveExecutions);
        Assert.Equal(0, otherExecutions);
        Assert.Equal(HttpStatusCode.OK, livenessResponse.StatusCode);

        var healthResponse = await client.GetAsync("/health");

        Assert.Equal(1, otherExecutions);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, healthResponse.StatusCode);
    }

    [Fact]
    public async Task ConfigureMapEndpointRoute_Aspire模式HealthCheckPath非法_抛出GirvsException()
    {
        SetupSingletonAppSettings(
            new ServiceGovernanceConfig
            {
                ServiceDiscoveryProvider = ServiceDiscoveryProvider.Aspire,
                HealthCheckPath = "health",
            }
        );
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddHealthChecks();
        await using var application = builder.Build();

        var exception = Record.Exception(
            () => new ServiceGovernanceModule().ConfigureMapEndpointRoute(application)
        );

        Assert.IsType<GirvsException>(exception);
        Assert.Contains("HealthCheckPath", exception.Message);
    }

    [Fact]
    public async Task ConfigureMapEndpointRoute_Kubernetes模式LivenessCheckPath非法_抛出GirvsException()
    {
        SetupSingletonAppSettings(
            new ServiceGovernanceConfig
            {
                ServiceDiscoveryProvider = ServiceDiscoveryProvider.Kubernetes,
                LivenessCheckPath = "alive",
            }
        );
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddHealthChecks();
        await using var application = builder.Build();

        var exception = Record.Exception(
            () => new ServiceGovernanceModule().ConfigureMapEndpointRoute(application)
        );

        Assert.IsType<GirvsException>(exception);
        Assert.Contains("LivenessCheckPath", exception.Message);
    }

    [Fact]
    public async Task ConfigureMapEndpointRoute_Consul模式注册地址为空且路径非法_抛出GirvsException()
    {
        SetupSingletonAppSettings(
            new ServiceGovernanceConfig
            {
                ServiceDiscoveryProvider = ServiceDiscoveryProvider.Consul,
                ConsulAddress = "http://127.0.0.1:8500",
                ConsulRegistrationAddress = "",
                HealthCheckPath = "",
            }
        );
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddHealthChecks();
        await using var application = builder.Build();

        var exception = Record.Exception(
            () => new ServiceGovernanceModule().ConfigureMapEndpointRoute(application)
        );

        Assert.IsType<GirvsException>(exception);
        Assert.Contains("HealthCheckPath", exception.Message);
    }

    [Fact]
    public void ConsulRegistrationAddress为空时跳过注册()
    {
        var config = CreateConsulConfigWithEmptyRegistrationAddress();
        SetupSingletonAppSettings(config);
        var registrar = new CountingConsulServiceRegistrar();
        var loggerProvider = new MemoryLoggerProvider();
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(loggerProvider));
        services.AddSingleton<IConsulServiceRegistrar>(registrar);
        var application = new ApplicationBuilder(services.BuildServiceProvider());

        var exception = Record.Exception(
            () => new ServiceGovernanceModule().Configure(application, null)
        );

        Assert.Null(exception);
        Assert.Equal(0, registrar.RegisterCount);
        Assert.Contains(
            loggerProvider.Logs,
            entry =>
                entry.Level == LogLevel.Warning
                && entry.Message.Contains("ConsulRegistrationAddress", StringComparison.Ordinal)
        );
    }

    [Fact]
    public void ConsulRegistrationAddress非法时Configure抛出GirvsException()
    {
        var config = CreateConsulConfigWithEmptyRegistrationAddress();
        config.ConsulRegistrationAddress = "not-a-uri";
        SetupSingletonAppSettings(config);
        var registrar = new CountingConsulServiceRegistrar();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConsulServiceRegistrar>(registrar);
        var application = new ApplicationBuilder(services.BuildServiceProvider());

        var exception = Record.Exception(
            () => new ServiceGovernanceModule().Configure(application, null)
        );

        Assert.IsType<GirvsException>(exception);
        Assert.Equal(0, registrar.RegisterCount);
    }

    [Fact]
    public void LivenessCheckPath不以斜杠开头时抛出GirvsException()
    {
        var config = new ServiceGovernanceConfig
        {
            ServiceDiscoveryProvider = ServiceDiscoveryProvider.Consul,
            ConsulAddress = "http://127.0.0.1:8500",
            ConsulRegistrationAddress = "http://127.0.0.1:5080",
            LivenessCheckPath = "alive",
        };
        SetupSingletonAppSettings(config);
        var services = new ServiceCollection();
        services.AddLogging();
        var application = new ApplicationBuilder(services.BuildServiceProvider());

        var exception = Record.Exception(
            () => new ServiceGovernanceModule().Configure(application, null)
        );

        Assert.IsType<GirvsException>(exception);
        Assert.Contains("LivenessCheckPath", exception.Message);
    }

    private static ServiceGovernanceConfig CreateConsulConfigWithEmptyRegistrationAddress() =>
        new()
        {
            ServiceDiscoveryProvider = ServiceDiscoveryProvider.Consul,
            ConsulAddress = "http://127.0.0.1:8500",
            ConsulRegistrationAddress = "",
        };

    private sealed class CountingConsulServiceRegistrar : IConsulServiceRegistrar
    {
        public int RegisterCount { get; private set; }

        public void Register(ServiceGovernanceConfig config) => RegisterCount++;
    }

    private sealed class MemoryLoggerProvider : ILoggerProvider
    {
        public List<(LogLevel Level, string Message)> Logs { get; } = new();

        public ILogger CreateLogger(string categoryName) => new MemoryLogger(this);

        public void Dispose() { }

        private sealed class MemoryLogger : ILogger
        {
            private readonly MemoryLoggerProvider _provider;

            public MemoryLogger(MemoryLoggerProvider provider) => _provider = provider;

            public IDisposable BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception exception,
                Func<TState, Exception, string> formatter
            ) => _provider.Logs.Add((logLevel, formatter(state, exception)));
        }
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
