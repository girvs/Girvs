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

        if (string.IsNullOrWhiteSpace(config.ConsulRegistrationAddress))
        {
            logger.LogWarning("Consul 模式已启用但 ConsulRegistrationAddress 为空，跳过服务注册");
            return;
        }

        if (
            !Uri.TryCreate(config.ConsulRegistrationAddress, UriKind.Absolute, out var registrationUri)
            || (registrationUri.Scheme != Uri.UriSchemeHttp && registrationUri.Scheme != Uri.UriSchemeHttps)
        )
        {
            throw new GirvsException(
                $"ConsulRegistrationAddress 必须为合法 http/https 绝对 URI：{config.ConsulRegistrationAddress}"
            );
        }

        ValidateHealthPath(config.HealthCheckPath, nameof(config.HealthCheckPath));
        ValidateHealthPath(config.LivenessCheckPath, nameof(config.LivenessCheckPath));

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
        var config = Singleton<AppSettings>.Instance.Get<ServiceGovernanceConfig>();
        // 无论 Provider（Aspire/Kubernetes/Consul）或 ConsulRegistrationAddress 是否为空，
        // 端点映射前都必须保证路径合法，避免挂载无效健康检查路由
        ValidateHealthPath(config.HealthCheckPath, nameof(config.HealthCheckPath));
        ValidateHealthPath(config.LivenessCheckPath, nameof(config.LivenessCheckPath));
        builder.MapHealthChecks(config.HealthCheckPath);
        builder.MapHealthChecks(
            config.LivenessCheckPath,
            new HealthCheckOptions
            {
                Predicate = registration => registration.Tags.Contains("live"),
            }
        );
    }

    public int Order => -10000;

    private static bool IsEventBusModuleLoaded() =>
        Type.GetType("Girvs.EventBus.EventBusModule, Girvs.EventBus") != null;

    private static void ValidateHealthPath(string path, string name)
    {
        if (string.IsNullOrWhiteSpace(path) || !path.StartsWith('/'))
            throw new GirvsException(
                $"{name} 必须非空且以 / 开头，当前值：{path}"
            );
    }

}
