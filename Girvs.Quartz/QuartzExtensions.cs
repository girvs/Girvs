namespace Girvs.Quartz;

public static class QuartzExtensions
{
    /// <summary>
    /// 基于 Quartz 官方 DI/Hosting 机制装载定时任务：
    /// 由 <c>MicrosoftDependencyInjectionJobFactory</c> 为每次触发自动创建作用域、从作用域解析 Job、执行后释放，
    /// 天然实现任务间的作用域隔离与连接释放，无需自定义 JobFactory / HostedService。
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
    }
}
