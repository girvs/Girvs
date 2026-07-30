namespace Girvs.ServiceGovernance.Discovery;

/// <summary>统一服务目录注册扩展。</summary>
public static class ServiceDirectoryServiceCollectionExtensions
{
    public static IServiceCollection AddGirvsServiceDirectory(
        this IServiceCollection services,
        ServiceGovernanceConfig config
    )
    {
        services.AddSingleton(config);
        services.AddSingleton<ServiceDirectory>();
        services.AddSingleton<IServiceDirectory>(provider => provider.GetRequiredService<ServiceDirectory>());
        services.AddSingleton<IServiceDirectoryHealth>(provider => provider.GetRequiredService<ServiceDirectory>());
        if (config.ServiceDiscoveryProvider == ServiceDiscoveryProvider.Consul)
            services.AddSingleton<IServiceDirectoryProvider, ConsulServiceDirectoryProvider>();
        else if (config.ServiceDiscoveryProvider == ServiceDiscoveryProvider.Kubernetes)
            services.AddSingleton<IServiceDirectoryProvider, KubernetesServiceDirectoryProvider>();
        else
            services.AddSingleton<IServiceDirectoryProvider, AspireServiceDirectoryProvider>();

        services.AddHostedService<ServiceDirectoryRefreshService>();
        services.AddHealthChecks().AddCheck<ServiceDirectoryHealthCheck>("service-directory", tags: ["ready"]);
        return services;
    }
}
