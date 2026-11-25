using Girvs;
using Girvs.Gateway.FlowProtection.Configuration;
using Girvs.Cache.CacheImps;
using Girvs.Cache.Configuration;
using Girvs.Configuration.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Transforms.Builder;

namespace Girvs.Gateway.FlowProtection;

/// <summary>
/// 流程防护的依赖注入注册入口。
/// </summary>
/// <remarks>
/// 注册策略遵循“始终可构造、按配置决定是否生效”：
/// 无论 <c>FlowProtection:Enabled</c> 为何值，中间件及其依赖都会被注册，宿主因此可以无条件地在
/// 反向代理中间件顺序中插入 <see cref="FlowTicketMiddleware"/>；是否真正校验由运行期的
/// <see cref="FlowProtectionOptions.Enabled"/> 决定。
/// </remarks>
public static class FlowProtectionServiceCollectionExtensions
{
    /// <summary>
    /// 注册流程防护所需的选项、流程定义、状态存储、票据服务与响应头转换。
    /// </summary>
    /// <param name="services">目标服务集合。</param>
    /// <param name="configuration">应用配置，用于读取 <c>FlowProtection</c> 节及校验缓存资源。</param>
    /// <returns>便于链式调用的原 <paramref name="services" /> 实例。</returns>
    /// <exception cref="GirvsException">
    /// 启用流程防护但分布式缓存未启用、未配置连接资源引用，或引用的资源缺失/类型不是 <c>redis</c>。
    /// </exception>
    /// <remarks>
    /// 禁用时不会校验业务流程配置，也不会解析 Redis 资源，因为此时任何校验都只会在部署环境里
    /// 制造与当前功能无关的启动失败。
    /// </remarks>
    public static IServiceCollection AddFlowProtection(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // 缺少配置节时用默认值（默认禁用），保证宿主可以无配置启动。
        var options = configuration.GetSection("FlowProtection").Get<FlowProtectionOptions>()
            ?? new FlowProtectionOptions();
        services.AddSingleton(options);

        // 无论是否启用都注册中间件的依赖：允许中间件顺序中无条件调用
        // UseMiddleware<FlowTicketMiddleware>()，由 options.Enabled 在运行期决定是否放行。
        // 禁用时用默认选项构建空注册表，避免校验尚未打算启用的业务流程配置。
        var registryOptions = options.Enabled ? options : new FlowProtectionOptions();
        services.AddSingleton(sp => new FlowDefinitionRegistry(registryOptions));
        services.AddSingleton<DownstreamPathResolver>();

        if (options.Enabled)
        {
            // 启用前先做资源校验，把配置缺失提前到启动期暴露，而不是等第一个受保护请求到来才失败。
            ValidateRedisConfiguration(configuration);
            services.AddSingleton<IFlowStateStore, RedisFlowStateStore>();
        }
        else
        {
            services.AddSingleton<IFlowStateStore>(DisabledFlowStateStore.Instance);
        }

        // 处理锁时长与选项保持一致；禁用时退化为默认 30 秒（占位实现不会被真正调用）。
        services.AddSingleton(sp => new FlowTicketService(
            sp.GetRequiredService<IFlowStateStore>(),
            options.Enabled ? options.LockTtlSeconds : 30
        ));

        if (!options.Enabled)
        {
            return services;
        }

        // 仅在启用时挂载响应头转换：负责提交状态并回写票据/顺位/完成标记。
        services.AddSingleton<ITransformProvider, FlowResponseTransform>();

        return services;
    }

    /// <summary>
    /// 禁用状态下的占位实现，保证中间件可被构造；实际不会被调用。
    /// </summary>
    /// <remarks>
    /// 所有操作统一返回“票据不存在”的失败结果。即便因为运行期误改配置而使调用路径被触发，
    /// 结果也是安全拒绝，而不是放行。
    /// </remarks>
    private sealed class DisabledFlowStateStore : IFlowStateStore
    {
        /// <summary>进程内共享的唯一实例，避免无谓分配。</summary>
        public static readonly DisabledFlowStateStore Instance = new();

        /// <summary>所有操作共用的失败结果（已完成的任务，可被并发等待）。</summary>
        private static readonly Task<FlowStoreResult> Result =
            Task.FromResult(FlowStoreResult.Failed(FlowErrorCode.FlowNotFound));

        /// <summary>禁用时不创建票据，直接返回失败结果。</summary>
        /// <param name="request">预留请求（忽略）。</param>
        /// <param name="cancellationToken">取消标记（忽略）。</param>
        /// <returns>固定为“票据不存在”的失败结果。</returns>
        public Task<FlowStoreResult> CreateAndReserveAsync(
            FlowReserveRequest request,
            CancellationToken cancellationToken
        ) => Result;

        /// <summary>禁用时不推进票据，直接返回失败结果。</summary>
        /// <param name="request">预留请求（忽略）。</param>
        /// <param name="cancellationToken">取消标记（忽略）。</param>
        /// <returns>固定为“票据不存在”的失败结果。</returns>
        public Task<FlowStoreResult> ReserveAsync(
            FlowReserveRequest request,
            CancellationToken cancellationToken
        ) => Result;

        /// <summary>禁用时不提交状态，直接返回失败结果。</summary>
        /// <param name="request">提交请求（忽略）。</param>
        /// <param name="cancellationToken">取消标记（忽略）。</param>
        /// <returns>固定为“票据不存在”的失败结果。</returns>
        public Task<FlowStoreResult> CommitAsync(
            FlowCommitRequest request,
            CancellationToken cancellationToken
        ) => Result;

        /// <summary>禁用时不释放锁，直接返回失败结果。</summary>
        /// <param name="request">释放请求（忽略）。</param>
        /// <param name="cancellationToken">取消标记（忽略）。</param>
        /// <returns>固定为“票据不存在”的失败结果。</returns>
        public Task<FlowStoreResult> ReleaseAsync(
            FlowReleaseRequest request,
            CancellationToken cancellationToken
        ) => Result;

        /// <summary>禁用时不删除票据，直接返回失败结果。</summary>
        /// <param name="request">删除请求（忽略）。</param>
        /// <param name="cancellationToken">取消标记（忽略）。</param>
        /// <returns>固定为“票据不存在”的失败结果。</returns>
        public Task<FlowStoreResult> DeleteAsync(
            FlowReleaseRequest request,
            CancellationToken cancellationToken
        ) => Result;
    }

    /// <summary>
    /// 校验流程防护所依赖的 Redis 资源已正确声明。
    /// </summary>
    /// <param name="configuration">应用配置。</param>
    /// <exception cref="GirvsException">
    /// 分布式缓存未启用、未配置连接资源引用、引用资源不存在，或资源类型不是 <c>redis</c>。
    /// </exception>
    /// <remarks>
    /// <para>
    /// 读取顺序与网关模块配置一致：先 <c>ModuleConfigurations:CacheConfig</c>，再顶级 <c>CacheConfig</c>，
    /// 最后回退默认实例。
    /// </para>
    /// <para>
    /// 流程票据依赖 Redis 的原子脚本语义，内存缓存或其它缓存实现无法提供等价的并发保证，
    /// 因此在启动期就拒绝非 <c>redis</c> 资源，而不是运行期降级。
    /// </para>
    /// </remarks>
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

        // 连接必须通过资源引用声明，禁止在此处内联连接串，保证凭据来源统一由资源管理。
        if (!InfrastructureResourceReferenceResolver.IsConfigured(distributedCache.ConnectionRef))
        {
            throw new GirvsException("FlowProtection 启用时 CacheConfig.DistributedCacheConfig.ConnectionRef 不能为空");
        }

        var resourceKey = InfrastructureResourceReferenceResolver.FormatResourceKey(
            distributedCache.ConnectionRef
        );
        var resources = configuration.GetSection("Resources").Get<Dictionary<string, GirvsInfrastructureResource>>() ?? [];
        // 引用必须能在 Resources 中解析到实体，否则注入了缓存配置也无法建立连接。
        if (!resources.TryGetValue(resourceKey, out var resource))
        {
            throw new GirvsException($"Resources:{resourceKey} 未配置");
        }

        if (!string.Equals(resource.Type, "redis", StringComparison.OrdinalIgnoreCase))
        {
            throw new GirvsException($"Resources:{resourceKey}:Type 必须为 redis");
        }

        // 不在此处检查 IRedisConnectionWrapper 是否已注册：GirvsCacheModule 在
        // ConfigureApplicationServices 阶段才注册，早于/晚于本方法的顺序取决于宿主写法。
        // 缺失时 RedisFlowStateStore 解析会给出清晰的 DI 异常。
    }
}
