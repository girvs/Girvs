namespace Girvs.ServiceGovernance.Services;

internal interface IConsulServiceRegistrar
{
    void Register(ServiceGovernanceConfig config);
}

internal sealed class ConsulServiceRegistrar(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime applicationLifetime,
    ILogger<ConsulServiceRegistrar> logger
) : IConsulServiceRegistrar
{
    public void Register(ServiceGovernanceConfig config)
    {
        if (config.CurrentServerModel == ConsulServerModel.GrpcService)
        {
            var client = serviceProvider.GetRequiredService<IConsulClient>();
            Register(client, CreateGrpcRegistration(config), config.ConsulAddress);
            return;
        }

        var webApiClient = new ConsulClient(options =>
            options.Address = new Uri(config.ConsulAddress)
        );
        Register(webApiClient, CreateWebApiRegistration(config), config.ConsulAddress);
    }

    internal static AgentServiceRegistration CreateWebApiRegistration(
        ServiceGovernanceConfig config
    )
    {
        var healthUri = new Uri(config.HealthAddress);
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
                HTTP = config.HealthAddress,
                Timeout = TimeSpan.FromSeconds(config.Timeout),
            }
        );
    }

    internal static AgentServiceRegistration CreateGrpcRegistration(
        ServiceGovernanceConfig config
    )
    {
        var healthUri = new Uri(config.HealthAddress);
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
                GRPC = config.HealthAddress.Replace($"{healthUri.Scheme}://", ""),
                Timeout = TimeSpan.FromSeconds(config.Timeout),
            }
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

    private void Register(
        IConsulClient client,
        AgentServiceRegistration registration,
        string consulAddress
    )
    {
        client.Agent.ServiceRegister(registration).GetAwaiter().GetResult();
        applicationLifetime.ApplicationStopping.Register(() =>
            Deregister(client, registration, consulAddress)
        );
    }

    private void Deregister(
        IConsulClient client,
        AgentServiceRegistration registration,
        string consulAddress
    )
    {
        try
        {
            client.Agent.ServiceDeregister(registration.ID).GetAwaiter().GetResult();
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
            : config.ServerName;
}
