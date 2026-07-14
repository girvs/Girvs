using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Girvs.Aspire.Tests;

public class AspireModuleTests
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
        foreach (var config in configs)
        {
            appSettings.ModuleConfigurations.Add(config.GetType().Name, config);
        }

        Singleton<AppSettings>.Instance = appSettings;
        return appSettings;
    }

    [Fact]
    public void 模块Order为极小值确保先于其他模块执行()
    {
        Assert.True(new AspireModule().Order < 0);
    }

    [Fact]
    public void ConfigureServices注册健康检查服务()
    {
        SetupSingletonAppSettings();
        var services = new ServiceCollection();
        var configuration = BuildConfiguration();

        new AspireModule().ConfigureServices(services, configuration);

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

            new AspireModule().ConfigureServices(services, BuildConfiguration());

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
