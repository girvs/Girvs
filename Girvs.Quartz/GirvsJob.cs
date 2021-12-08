using System;
using System.Threading.Tasks;
using Girvs.Infrastructure;
using Girvs.Infrastructure.GirvsServiceContext;
using Quartz;

namespace Girvs.Quartz
{
    public abstract class GirvsJob : IJob,IDisposable
    {
        public GirvsJob(
            IServiceProvider serviceProvider
        )
        {
            EngineContext.Current.SetCurrentThreadServiceProvider(serviceProvider);
        }

        public abstract Task Execute(IJobExecutionContext context);

        public void Dispose()
        {
            EngineContext.Current.SetCurrentThreadServiceProvider(null);
        }
    }
}