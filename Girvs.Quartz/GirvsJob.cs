using System;
using System.Threading.Tasks;
using Girvs.Infrastructure;
using Quartz;

namespace Girvs.Quartz
{
    public abstract class GirvsJob : IJob,IDisposable
    {
        private readonly IServiceProvider _serviceProvider;

        public GirvsJob(
            IServiceProvider serviceProvider
        )
        {
            _serviceProvider = serviceProvider;

        }

        public virtual Task Execute(IJobExecutionContext context)
        {
            EngineContext.Current.SetCurrentThreadServiceProvider(_serviceProvider);
            GirvsExecute(context);
            return Task.CompletedTask;
        }

        public abstract void GirvsExecute(IJobExecutionContext context);

        public virtual void Dispose()
        {
            EngineContext.Current.SetCurrentThreadServiceProvider(null);
        }
    }
}