using System;
using System.Linq;
using Girvs.Domain.Configuration;
using Girvs.Domain.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Quartz.Spi;

namespace Girvs.Service.FrameWork.Infrastructure.QuartzExtensions
{
    public static class QuartzExtensions
    {
        public static void AddQuartzHosted(this IServiceCollection services)
        {
            GirvsConfig config = EngineContext.Current.Resolve<GirvsConfig>();
            services.AddSingleton<IJobFactory, SingletonJobFactory>();
            var tasks = config.Tasks.Where(x => x.Enabled).ToList();
            foreach (var task in config.Tasks.Where(x => x.Enabled).ToList())
            {
                Type jobt = Type.GetType(task.Type);
                services.AddTransient(jobt);
            }

            if (tasks.Any())
                services.AddHostedService<QuartzHostedService>();
        }
    }
}