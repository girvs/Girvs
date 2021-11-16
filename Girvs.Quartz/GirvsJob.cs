using System;
using System.Threading.Tasks;
using Girvs.Infrastructure.GirvsServiceContext;
using Quartz;

namespace Girvs.Quartz
{
    public abstract class GirvsJob : IJob
    {
        public GirvsJob(
            IServiceProvider serviceProvider
        )
        {
            ServiceContextFactory.Create(serviceProvider);
        }

        public abstract Task Execute(IJobExecutionContext context);
    }
}