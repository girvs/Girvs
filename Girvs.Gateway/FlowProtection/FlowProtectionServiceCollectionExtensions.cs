using Girvs;
using Girvs.Aspire.Gateway.FlowProtection.Configuration;
using Girvs.Cache.CacheImps;
using Girvs.Cache.Configuration;
using Girvs.Configuration.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Transforms.Builder;

namespace Girvs.Aspire.Gateway.FlowProtection;

public static class FlowProtectionServiceCollectionExtensions
{
    public static IServiceCollection AddFlowProtection(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var options = configuration.GetSection("FlowProtection").Get<FlowProtectionOptions>()
            ?? new FlowProtectionOptions();
        services.AddSingleton(options);

        if (!options.Enabled)
        {
            return services;
        }

        ValidateRedisConfiguration(configuration, services);
        services.AddSingleton<FlowDefinitionRegistry>(sp =>
        {
            return new FlowDefinitionRegistry(options);
        });
        services.AddSingleton<IFlowStateStore, RedisFlowStateStore>();
        services.AddSingleton(sp => new FlowTicketService(
            sp.GetRequiredService<IFlowStateStore>(),
            options.LockTtlSeconds
        ));
        services.AddSingleton<DownstreamPathResolver>();
        services.AddSingleton<ITransformProvider, FlowResponseTransform>();

        return services;
    }

    private static void ValidateRedisConfiguration(
        IConfiguration configuration,
        IServiceCollection services
    )
    {
        var cacheConfig = configuration
            .GetSection("ModuleConfigurations:CacheConfig")
            .Get<CacheConfig>()
            ?? configuration.GetSection("CacheConfig").Get<CacheConfig>()
            ?? new CacheConfig();
        var distributedCache = cacheConfig.DistributedCacheConfig;
        if (!distributedCache.Enabled)
        {
            throw new GirvsException("FlowProtection 启用时必须启用 Girvs.Cache 分布式缓存");
        }

        if (string.IsNullOrWhiteSpace(distributedCache.ConnectionRef))
        {
            throw new GirvsException("FlowProtection 启用时 CacheConfig.DistributedCacheConfig.ConnectionRef 不能为空");
        }

        var resources = configuration.GetSection("Resources").Get<Dictionary<string, Resource>>() ?? [];
        if (!resources.TryGetValue(distributedCache.ConnectionRef, out var resource))
        {
            throw new GirvsException($"Resources:{distributedCache.ConnectionRef} 未配置");
        }

        if (!string.Equals(resource.Type, "redis", StringComparison.OrdinalIgnoreCase))
        {
            throw new GirvsException($"Resources:{distributedCache.ConnectionRef}:Type 必须为 redis");
        }

        if (!services.Any(x => x.ServiceType == typeof(IRedisConnectionWrapper)))
        {
            throw new GirvsException("FlowProtection 启用时必须注册 Girvs.Cache IRedisConnectionWrapper");
        }
    }
}
