﻿using System;
using System.Linq;
using Girvs.Quartz.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz.Spi;

namespace Girvs.Quartz
{
    public static class QuartzExtensions
    {
        public static void AddQuartzHosted(this IServiceCollection services,QuartzConfiguration configuration)
        {
            services.AddSingleton<IJobFactory, SingletonJobFactory>();
            var tasks = configuration.Tasks.Where(x => x.Enabled).ToList();
            foreach (var task in configuration.Tasks.Where(x => x.Enabled).ToList())
            {
                Type jobt = Type.GetType(task.Type);
                services.AddTransient(jobt);
            }

            if (tasks.Any())
                services.AddHostedService<QuartzHostedService>();
        }
    }
}