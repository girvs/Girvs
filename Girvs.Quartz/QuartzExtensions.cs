namespace Girvs.Quartz;

public static class QuartzExtensions
{
    public static void AddQuartzHosted(this IServiceCollection services, QuartzConfiguration configuration)
    {
        services.AddTransient<ISchedulerFactory, StdSchedulerFactory>();
        services.AddTransient<IJobFactory, SingletonJobFactory>();
        var tasks = configuration.Tasks.Where(x => x.Enabled).ToList();
        foreach (var task in configuration.Tasks.Where(x => x.Enabled).ToList())
        {
            var jobType = Type.GetType(task.Type);
            if (jobType != null) services.AddTransient(jobType);
        }

        if (tasks.Any())
            services.AddTransient<IHostedService, QuartzHostedService>();
    }
}