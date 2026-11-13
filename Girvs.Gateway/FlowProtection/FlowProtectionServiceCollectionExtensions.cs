using Girvs;
using Girvs.Gateway.FlowProtection.Configuration;
using Girvs.Cache.CacheImps;
using Girvs.Cache.Configuration;
using Girvs.Configuration.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Transforms.Builder;

namespace Girvs.Gateway.FlowProtection;

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

        // 无论是否启用都注册中间件的依赖：允许管线中无条件调用
        // UseMiddleware<FlowTicketMiddleware>()，由 options.Enabled 在运行期决定是否放行。
        // 禁用时用默认选项构建空注册表，避免校验尚未打算启用的业务流程配置。
        var registryOptions = options.Enabled ? options : new FlowProtectionOptions();
        services.AddSingleton(sp => new FlowDefinitionRegistry(registryOptions));
        services.AddSingleton<DownstreamPathResolver>();

        if (options.Enabled)
        {
            ValidateRedisConfiguration(configuration);
            services.AddSingleton<IFlowStateStore, RedisFlowStateStore>();
        }
        else
        {
            services.AddSingleton<IFlowStateStore>(DisabledFlowStateStore.Instance);
        }

        services.AddSingleton(sp => new FlowTicketService(
            sp.GetRequiredService<IFlowStateStore>(),
            options.Enabled ? options.LockTtlSeconds : 30
        ));

        if (!options.Enabled)
        {
            return services;
        }

        services.AddSingleton<ITransformProvider, FlowResponseTransform>();

        return services;
    }

    /// <summary>
    /// 禁用状态下的占位实现，保证中间件可被构造；实际不会被调用。
    /// </summary>
    private sealed class DisabledFlowStateStore : IFlowStateStore
    {
        public static readonly DisabledFlowStateStore Instance = new();

        private static readonly Task<FlowStoreResult> Result =
            Task.FromResult(FlowStoreResult.Failed(FlowErrorCode.FlowNotFound));

        public Task<FlowStoreResult> CreateAndReserveAsync(
            FlowReserveRequest request,
            CancellationToken cancellationToken
        ) => Result;

        public Task<FlowStoreResult> ReserveAsync(
            FlowReserveRequest request,
            CancellationToken cancellationToken
        ) => Result;

        public Task<FlowStoreResult> CommitAsync(
            FlowCommitRequest request,
            CancellationToken cancellationToken
        ) => Result;

        public Task<FlowStoreResult> ReleaseAsync(
            FlowReleaseRequest request,
            CancellationToken cancellationToken
        ) => Result;

        public Task<FlowStoreResult> DeleteAsync(
            FlowReleaseRequest request,
            CancellationToken cancellationToken
        ) => Result;
    }

    private static void ValidateRedisConfiguration(IConfiguration configuration)
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

        var resources = configuration.GetSection("Resources").Get<Dictionary<string, GirvsInfrastructureResource>>() ?? [];
        if (!resources.TryGetValue(distributedCache.ConnectionRef, out var resource))
        {
            throw new GirvsException($"Resources:{distributedCache.ConnectionRef} 未配置");
        }

        if (!string.Equals(resource.Type, "redis", StringComparison.OrdinalIgnoreCase))
        {
            throw new GirvsException($"Resources:{distributedCache.ConnectionRef}:Type 必须为 redis");
        }

        // 不在此处检查 IRedisConnectionWrapper 是否已注册：GirvsCacheModule 在
        // ConfigureApplicationServices 阶段才注册，早于/晚于本方法的顺序取决于宿主写法。
        // 缺失时 RedisFlowStateStore 解析会给出清晰的 DI 异常。
    }
}
