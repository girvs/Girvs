namespace Girvs.Quartz;

/// <summary>
/// Girvs 包装型 JobFactory：每次 Job 触发时创建独立 DI 作用域，
/// 并在真实 Job 的解析与整个执行期内把 EngineContext 桥接到该作用域的 ServiceProvider，
/// 使 Job 构造链（含 Repository 构造器内部经 EngineContext 的 DbContext 解析）全部命中本次触发的作用域，
/// 执行结束后释放作用域。以此消除旧机制回落根容器导致的「根作用域 DbContext 被多线程共享且永不释放」问题。
/// </summary>
/// <remarks>
/// 使用契约：Job 类型必须预先注册于 DI 容器（如 AddScoped/AddTransient）。
/// 未注册的 Job 触发时会抛出 InvalidOperationException（GetRequiredService 无法解析）。
/// 此行为与官方默认工厂 MicrosoftDependencyInjectionJobFactory 不同：后者对未注册的 Job 会经 ActivatorUtilities 直接落建实例。
/// </remarks>
public class GirvsJobFactory : IJobFactory
{
    private readonly IServiceProvider _rootProvider;
    private readonly ILogger<GirvsJobFactory> _logger;

    public GirvsJobFactory(IServiceProvider rootProvider, ILogger<GirvsJobFactory> logger)
    {
        _rootProvider = rootProvider ?? throw new ArgumentNullException(nameof(rootProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
    {
        // 只创建独立作用域，不在此时解析真实 Job：
        // 保证 Job 及其构造链（Repository / DbContext）在 AsyncLocal 桥接之后才从本作用域解析
        var scope = _rootProvider.CreateScope();
        try
        {
            return new ScopedGirvsJobWrapper(scope, bundle.JobDetail.JobType, _logger);
        }
        catch
        {
            // 防御性加固：后续构造若抛异常，Quartz 不会调用 ReturnJob，作用域将泄漏；此处先行释放后原样上抛
            scope.Dispose();
            throw;
        }
    }

    public void ReturnJob(IJob job)
    {
        // Quartz 在每次触发执行结束（无论成功或异常）后归还 Job，此处释放本次触发的作用域
        (job as IDisposable)?.Dispose();
    }

    /// <summary>
    /// 持有本次触发专属作用域的包装 Job：执行时先桥接 EngineContext，再从作用域解析真实 Job。
    /// </summary>
    private sealed class ScopedGirvsJobWrapper(IServiceScope scope, Type jobType, ILogger logger)
        : IJob, IDisposable
    {
        public async Task Execute(IJobExecutionContext context)
        {
            // AsyncLocal 桥接：using 结束即把 EngineContext 还原为进入前的 provider。
            // Job 内禁止 fire-and-forget 超出 Execute 生命周期的 scoped 操作：
            // 异步流脱离本上下文后 EngineContext 将回退根容器，scoped 服务亦随作用域释放而不可用。
            using var _ = EngineContext.Current.ChangeCurrentThreadServiceProvider(scope.ServiceProvider);

            // 真实 Job 在桥接之后才解析，其构造器经 EngineContext 取得的 DbContext 等 Scoped 服务落在本作用域
            var job = (IJob)scope.ServiceProvider.GetRequiredService(jobType);

            logger.LogDebug("定时任务开始执行 {JobType}", jobType.FullName);
            // 异常不吞，原样上抛交给 Quartz 处置
            await job.Execute(context);
            logger.LogDebug("定时任务执行结束 {JobType}", jobType.FullName);
        }

        public void Dispose() => scope.Dispose();
    }
}
