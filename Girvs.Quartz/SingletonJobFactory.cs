namespace Girvs.Quartz;

public class SingletonJobFactory : IJobFactory
{
    private readonly IServiceProvider _serviceProvider;
    public SingletonJobFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
    {
        return _serviceProvider.CreateScope().ServiceProvider.GetRequiredService(bundle.JobDetail.JobType) as IJob;
    }

    public void ReturnJob(IJob job)
    {
        (job as IDisposable)?.Dispose();
    }
}