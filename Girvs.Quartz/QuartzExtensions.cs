using System;
using System.Linq;
using Girvs.Quartz.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;

namespace Girvs.Quartz
{
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
}