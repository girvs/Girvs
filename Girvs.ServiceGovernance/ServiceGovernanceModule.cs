using Girvs.ServiceGovernance.Services;
using Girvs.ServiceGovernance.Discovery;

namespace Girvs.ServiceGovernance;

/// <summary>
/// 服务治理模块：OpenTelemetry、Aspire 服务发现、HttpClient 弹性与健康检查。
/// 各组件的 Aspire 连接串覆盖由组件自身负责（如 CacheConfig.ApplyAspireConnectionString），
/// 本模块不做集中映射。
/// </summary>
public class ServiceGovernanceModule : IAppModuleStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var config = Singleton<AppSettings>.Instance.Get<ServiceGovernanceConfig>();
        services.AddGirvsServiceDirectory(config);
        if (config.ServiceDiscoveryProvider == ServiceDiscoveryProvider.Aspire)
        {
            services.AddServiceDiscovery();
            services.ConfigureHttpClientDefaults(http =>
            {
                http.AddStandardResilienceHandler();
                http.AddServiceDiscovery();
            });
        }
        else
        {
            services.AddSingleton<IConsulServiceRegistrar, ConsulServiceRegistrar>();
            if (config.CurrentServerModel == ConsulServerModel.GrpcService)
            {
                services.AddSingleton<IConsulClient>(
                    _ =>
                        new ConsulClient(options =>
                        {
                            options.Address = new Uri(config.ConsulAddress);
                        })
                );
            }
        }

        services
            .AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

        if (
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT"))
        )
        {
            services
                .AddOpenTelemetry()
                .WithMetrics(metrics =>
                    metrics
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation()
                )
                .WithTracing(tracing =>
                {
                    tracing.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation();
                    if (IsEventBusModuleLoaded())
                        tracing.AddCapInstrumentation();
                })
                .UseOtlpExporter();
        }
    }

    public void Configure(IApplicationBuilder application, IWebHostEnvironment env)
    {
        var config = Singleton<AppSettings>.Instance.Get<ServiceGovernanceConfig>();
        if (config.ServiceDiscoveryProvider != ServiceDiscoveryProvider.Consul)
            return;

        var logger = application.ApplicationServices.GetRequiredService<
            ILogger<ServiceGovernanceModule>
        >();
        if (string.IsNullOrWhiteSpace(config.ConsulAddress))
        {
            logger.LogWarning("Consul 模式已启用但 ConsulAddress 为空，跳过服务注册");
            return;
        }

        try
        {
            application
                .ApplicationServices.GetRequiredService<IConsulServiceRegistrar>()
                .Register(config);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Consul 服务注册失败：服务 {ServerName}，Consul 地址 {ConsulAddress}",
                config.ServerName,
                config.ConsulAddress
            );
        }
    }

    public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
    {
        builder.MapHealthChecks("/health");
        builder.MapHealthChecks(
            "/alive",
            new HealthCheckOptions
            {
                Predicate = registration => registration.Tags.Contains("live"),
            }
        );
    }

    public int Order => -10000;

    private static bool IsEventBusModuleLoaded() =>
        Type.GetType("Girvs.EventBus.EventBusModule, Girvs.EventBus") != null;

}
