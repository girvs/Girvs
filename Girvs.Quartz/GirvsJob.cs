namespace Girvs.Quartz;

public abstract class GirvsJob(IServiceProvider serviceProvider) : IJob
{
    public virtual async Task Execute(IJobExecutionContext context)
    {
        // serviceProvider 由 Quartz 官方 MicrosoftDependencyInjectionJobFactory 从本次触发的作用域注入；
        // 作用域的创建与释放由 Quartz 负责，这里只把它桥接到 Girvs 的环境服务定位器（EngineContext），
        // 令牌在异步执行期切换、结束后还原，避免污染并发上下文。
        using var _ = EngineContext.Current.ChangeCurrentThreadServiceProvider(serviceProvider);
        await GirvsExecuteAsync(context);
    }

    public abstract Task GirvsExecuteAsync(IJobExecutionContext context);
}
