using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Girvs.Domain.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;

namespace Girvs.WebFrameWork.Infrastructure.QuartzExtensions
{
    public class QuartzHostedService : IHostedService
    {
        private readonly IJobFactory _jobFactory;
        private readonly ILogger<QuartzHostedService> _logger;
        private readonly GirvsConfig _girvsConfig;

        public QuartzHostedService(IJobFactory jobFactory, ILogger<QuartzHostedService> logger, GirvsConfig girvsConfig)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _girvsConfig = girvsConfig ?? throw new ArgumentNullException(nameof(girvsConfig));
            _jobFactory = jobFactory ?? throw new ArgumentNullException(nameof(jobFactory));
        }

        public IScheduler Scheduler { get; set; }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!_girvsConfig.Tasks.Any(x => x.Enabled)) return;
            _logger.LogInformation(@"
★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★
                            系统任务调度管理中心
");
            _logger.LogInformation("开始装载任务");
            _logger.LogInformation($"任务个数：{_girvsConfig.Tasks.Count}");

            Scheduler = await StdSchedulerFactory.GetDefaultScheduler(cancellationToken);
            Scheduler.JobFactory = _jobFactory;

            foreach (var task in _girvsConfig.Tasks.Where(x => x.Enabled))
            {
                _logger.LogInformation($"开始装载当前任务:{System.Text.Json.JsonSerializer.Serialize(task)}");
                Type t = Type.GetType(task.Type);
                var job = CreateJob(task, t);
                var trigger = CreateTrigger(task, t);
                await Scheduler.ScheduleJob(job, trigger, cancellationToken);
            }

            _logger.LogInformation("装载任务结束，开始启动任务");
            await Scheduler.Start(cancellationToken);

            _logger.LogInformation(@"
★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★
");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Scheduler?.Shutdown(cancellationToken);
        }

        private IJobDetail CreateJob(TaskConfig task, Type jobType)
        {
            return JobBuilder
                .Create(jobType)
                .WithIdentity(jobType.FullName)
                .WithDescription(jobType.Name)
                .Build();
        }

        private ITrigger CreateTrigger(TaskConfig task, Type jobType)
        {
            return TriggerBuilder
                .Create()
                .WithIdentity($"{jobType.FullName}.trigger")
                .WithCronSchedule(task.CronExpression)
                .WithDescription(task.CronExpression)
                .Build();
        }
    }
}