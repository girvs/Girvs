namespace Girvs.Quartz;

public abstract class GirvsJob(IServiceProvider serviceProvider) : IJob
{
    public virtual async Task Execute(IJobExecutionContext context)
    {
        // 不依赖 Quartz 传入的作用域是否被复用；每次触发都建立独立作用域，
        // 并在整个异步执行期桥接到 EngineContext，结束后自动还原。
        using var scope = serviceProvider.CreateScope();
        using var _ = EngineContext.Current.ChangeCurrentThreadServiceProvider(scope.ServiceProvider);
        await GirvsExecuteAsync(context);
    }

    public abstract Task GirvsExecuteAsync(IJobExecutionContext context);
}
