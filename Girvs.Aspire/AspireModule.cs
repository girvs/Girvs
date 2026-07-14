namespace Girvs.Aspire;

/// <summary>
/// Aspire 集成模块：连接串映射、OpenTelemetry、Aspire 服务发现、HttpClient 弹性与健康检查。
/// Order 取极小值，确保连接串映射先于其他模块消费配置。
/// </summary>
public class AspireModule : IAppModuleStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        AspireConnectionStringMapper.Apply(configuration, Singleton<AppSettings>.Instance);

        services.AddServiceDiscovery();
        services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        services
            .AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" });

        if (
            !string.IsNullOrEmpty(
                Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
            )
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

        WarnWhenConsulCoexists();
    }

    public void Configure(IApplicationBuilder application, IWebHostEnvironment env) { }

    public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
    {
        builder.MapHealthChecks("/health");
        builder.MapHealthChecks(
            "/alive",
            new HealthCheckOptions
            {
                Predicate = registration => registration.Tags.Contains("live")
            }
        );
    }

    public int Order => -10000;

    private static bool IsEventBusModuleLoaded() =>
        Type.GetType("Girvs.EventBus.EventBusModule, Girvs.EventBus") != null;

    private static void WarnWhenConsulCoexists()
    {
        if (Type.GetType("Girvs.Consul.ConsulModule, Girvs.Consul") != null)
        {
            Log.Warning(
                "检测到 Girvs.Consul 与 Girvs.Aspire 同时启用：两者的服务发现机制互斥，请仅保留其一（迁移期间可忽略此警告）"
            );
        }
    }
}
