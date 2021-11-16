using System;
using System.Linq;
using Girvs.Quartz.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz.Spi;

namespace Girvs.Quartz
{
    public static class QuartzExtensions
    {
        public static void AddQuartzHosted(this IServiceCollection services,QuartzConfiguration configuration)
        {
            services.AddTransient<IJobFactory, SingletonJobFactory>();
            var tasks = configuration.Tasks.Where(x => x.Enabled).ToList();
            foreach (var task in configuration.Tasks.Where(x => x.Enabled).ToList())
            {
                var jobt = Type.GetType(task.Type);
                services.AddTransient(jobt);
            }

            if (tasks.Any())
                services.AddTransient<IHostedService,QuartzHostedService>();
        }
    }
}