using System;
using System.Linq;
using Girvs.Domain.Configuration;
using Girvs.Domain.Infrastructure.DependencyManagement;
using Girvs.Domain.TypeFinder;
using Microsoft.Extensions.DependencyInjection;
using Quartz.Spi;

namespace Girvs.WebFrameWork.Infrastructure.QuartzExtensions
{
    public class QuartzRegistrar : IDependencyRegistrar
    {
        public void Register(IServiceCollection services, ITypeFinder typeFinder, GirvsConfig config)
        {
            services.AddSingleton<IJobFactory, SingletonJobFactory>();
            var tasks = config.Tasks.Where(x => x.Enabled).ToList();
            foreach (var task in config.Tasks.Where(x => x.Enabled).ToList())
            {
                Type jobt = Type.GetType(task.Type);
                services.AddTransient(jobt);
            }

            // var jobs = typeFinder.FindClassesOfType<IJob>();
            // foreach (var job in jobs)
            // {
            //     
            //     services.AddTransient(job);
            // }
            if (tasks.Any())
                services.AddHostedService<QuartzHostedService>();
        }

        public int Order { get; } = 1233;
    }
}