namespace Girvs.Quartz;

public static class QuartzExtensions
{
    /// <summary>
    /// 基于 Quartz 官方 DI/Hosting 机制装载定时任务。
    /// 官方默认的 <c>MicrosoftDependencyInjectionJobFactory</c> 虽为每次触发创建作用域，
    /// 但只作用于 Job 本身的解析：Job 内部经 <c>EngineContext</c> 的服务定位（如 Repository 构造器取 DbContext）
    /// 仍会回落到根容器，导致根作用域 DbContext 被多线程共享且永不释放。
    /// 故此处将其替换为 <see cref="GirvsJobFactory"/>：每次触发创建独立作用域，
    /// 并在整个执行期把 EngineContext 桥接到该作用域，执行结束释放，实现彻底的作用域隔离。
    /// </summary>
    public static void AddQuartzHosted(this IServiceCollection services, QuartzConfiguration configuration)
    {
        var tasks = configuration.Tasks.Where(x => x.Enabled).ToList();
        if (!tasks.Any()) return;

        services.AddQuartz(q =>
        {
            foreach (var task in tasks)
            {
                var jobType = Type.GetType(task.Type);
                if (jobType == null) continue;

                var jobKey = new JobKey(jobType.FullName!);

                q.AddJob(jobType, jobKey, j => j
                    .WithIdentity(jobKey)
                    .WithDescription(jobType.Name)
                    .StoreDurably());

                q.AddTrigger(t => t
                    .ForJob(jobKey)
                    .WithIdentity($"{jobType.FullName}.trigger")
                    .WithDescription(task.CronExpression)
                    .WithCronSchedule(task.CronExpression));
            }
        });

        services.AddQuartzHostedService(o => o.WaitForJobsToComplete = true);

        // AddQuartz 内部以单例注册了默认的 MicrosoftDependencyInjectionJobFactory，
        // 此处在其后统一替换为 GirvsJobFactory（三个 Quartz 版本 3.8.1 / 3.18.2 / 3.22.10 均为
        // IJobFactory 单例注册，Replace 移除默认实现并保证替换在各目标框架下生效）
        services.Replace(ServiceDescriptor.Singleton<IJobFactory, GirvsJobFactory>());
    }
}
