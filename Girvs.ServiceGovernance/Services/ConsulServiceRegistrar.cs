using Girvs;

namespace Girvs.ServiceGovernance.Services;

internal interface IConsulServiceRegistrar
{
    void Register(ServiceGovernanceConfig config);
}

internal sealed class ConsulServiceRegistrar(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime applicationLifetime,
    ILogger<ConsulServiceRegistrar> logger
) : IConsulServiceRegistrar, IDisposable
{
    /// <summary>
    /// Consul 注册/注销的等待上限。Consul 不可达时 HttpClient 默认要等 100 秒，
    /// 启动与停止阶段不能让进程因此长时间挂起。
    /// </summary>
    private const int ConsulOperationTimeoutSeconds = 5;

    // WebApi 模式由本类自建 client，注销回调仍需使用，生命周期覆盖整个应用；
    // 由本类持有并在容器释放时 Dispose，避免 HttpClient 及其 handler 泄漏。
    private IConsulClient _ownedClient;

    public void Register(ServiceGovernanceConfig config)
    {
        var client = ResolveClient(config);
        var registration =
            config.CurrentServerModel == ConsulServerModel.GrpcService
                ? CreateGrpcRegistration(config)
                : CreateWebApiRegistration(config);

        var registerTask = client.Agent.ServiceRegister(registration);
        if (!registerTask.Wait(TimeSpan.FromSeconds(ConsulOperationTimeoutSeconds)))
        {
            logger.LogWarning(
                "Consul 服务注册超时（{TimeoutSeconds} 秒），注册任务仍在后台继续：服务 {ServerName}，Consul 地址 {ConsulAddress}",
                ConsulOperationTimeoutSeconds,
                registration.Name,
                config.ConsulAddress
            );
        }

        applicationLifetime.ApplicationStopping.Register(() =>
            Deregister(client, registration, config.ConsulAddress)
        );
    }

    public void Dispose()
    {
        var client = Interlocked.Exchange(ref _ownedClient, null);
        if (client is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private IConsulClient ResolveClient(ServiceGovernanceConfig config)
    {
        if (config.CurrentServerModel == ConsulServerModel.GrpcService)
        {
            return serviceProvider.GetRequiredService<IConsulClient>();
        }

        var ownedClient = new ConsulClient(options =>
            options.Address = new Uri(config.ConsulAddress)
        );
        var previousClient = Interlocked.Exchange(ref _ownedClient, ownedClient);
        if (previousClient is IDisposable disposable)
        {
            disposable.Dispose();
        }
        return ownedClient;
    }

    internal static AgentServiceRegistration CreateWebApiRegistration(
        ServiceGovernanceConfig config
    )
    {
        var healthUri = ResolveRegistrationUri(config);
        ValidateHealthCheckPath(config.HealthCheckPath);
        var checkUrl =
            $"{healthUri.GetComponents(UriComponents.SchemeAndServer, UriFormat.UriEscaped)}{config.HealthCheckPath}";
        return CreateRegistration(
            config,
            healthUri,
            ".net Core WebApiService",
            new AgentServiceCheck
            {
                DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(
                    config.DeregisterCriticalServiceAfter
                ),
                Interval = TimeSpan.FromSeconds(config.Interval),
                HTTP = checkUrl,
                Timeout = TimeSpan.FromSeconds(config.Timeout),
            }
        );
    }

    internal static AgentServiceRegistration CreateGrpcRegistration(
        ServiceGovernanceConfig config
    )
    {
        var healthUri = ResolveRegistrationUri(config);
        return CreateRegistration(
            config,
            healthUri,
            ".net Core GrpcService",
            new AgentServiceCheck
            {
                DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(
                    config.DeregisterCriticalServiceAfter
                ),
                Interval = TimeSpan.FromSeconds(config.Interval),
                GRPC = $"{healthUri.Host}:{healthUri.Port}",
                Timeout = TimeSpan.FromSeconds(config.Timeout),
            }
        );
    }

    private static Uri ResolveRegistrationUri(ServiceGovernanceConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.ConsulRegistrationAddress))
            throw new GirvsException("ConsulRegistrationAddress 为空，无法生成 Consul 注册信息");
        if (
            !Uri.TryCreate(config.ConsulRegistrationAddress, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        )
            throw new GirvsException(
                $"ConsulRegistrationAddress 必须为合法 http/https 绝对 URI：{config.ConsulRegistrationAddress}"
            );
        return uri;
    }

    private static void ValidateHealthCheckPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !path.StartsWith('/'))
            throw new GirvsException(
                $"HealthCheckPath 必须非空且以 / 开头，当前值：{path}"
            );
    }

    private static AgentServiceRegistration CreateRegistration(
        ServiceGovernanceConfig config,
        Uri healthUri,
        string tag,
        AgentServiceCheck check
        )
    {
        var serverName = GetServerName(config);
        config.Services.TryGetValue(serverName, out var metadata);
        var tags = new List<string>
        {
            tag,
            "girvs.business=true",
            "girvs.endpoint=http",
        };
        if (metadata?.GatewayEnabled == true)
        {
            tags.Add("girvs.gateway-enabled=true");
            if (!string.IsNullOrWhiteSpace(metadata.GatewayEndpointName))
                tags.Add($"girvs.gateway-endpoint={metadata.GatewayEndpointName}");
        }

        return new AgentServiceRegistration
        {
            ID = Guid.NewGuid().ToString(),
            Tags = tags.ToArray(),
            Name = serverName,
            Address = healthUri.Host,
            Port = healthUri.Port,
            Check = check,
        };
    }

    private void Deregister(
        IConsulClient client,
        AgentServiceRegistration registration,
        string consulAddress
    )
    {
        try
        {
            var deregisterTask = client.Agent.ServiceDeregister(registration.ID);
            if (!deregisterTask.Wait(TimeSpan.FromSeconds(ConsulOperationTimeoutSeconds)))
            {
                logger.LogWarning(
                    "Consul 服务注销超时（{TimeoutSeconds} 秒）：服务 {ServerName}，Consul 地址 {ConsulAddress}",
                    ConsulOperationTimeoutSeconds,
                    registration.Name,
                    consulAddress
                );
            }
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Consul 服务注销失败：服务 {ServerName}，Consul 地址 {ConsulAddress}",
                registration.Name,
                consulAddress
            );
        }
    }

    private static string GetServerName(ServiceGovernanceConfig config) =>
        string.IsNullOrWhiteSpace(config.ServerName)
            ? ServiceNameResolver.FromAssemblyName()
            : ServiceNameResolver.FromServerName(config.ServerName);
}
