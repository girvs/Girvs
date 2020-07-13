using System;
using System.Threading;
using System.Threading.Tasks;
using Hdyt.SmartProducts.Core.Caching;
using Microsoft.Extensions.Logging;
using Quartz;
using SmartProducts.Person.Core.Managers;

namespace SmartProducts.Person.WebApi.Jobs
{
    //[DisallowConcurrentExecution]
    public class TestJob : IJob
    {
        private readonly IPersonInfoManager _personInfoService;
        private readonly ILogger<TestJob> _logger;
        private readonly ICacheUsingManager _cacheUsingManager;

        public TestJob(IPersonInfoManager personInfoService, ILogger<TestJob> logger,
            ICacheUsingManager cacheUsingManager)
        {
            _personInfoService = personInfoService ?? throw new ArgumentNullException(nameof(personInfoService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cacheUsingManager = cacheUsingManager ?? throw new ArgumentNullException(nameof(cacheUsingManager));
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var list = await _cacheUsingManager.GetAllKeysAsync();
            _logger.LogInformation(System.Text.Json.JsonSerializer.Serialize(list));
            Thread.Sleep(10000);
        }
    }
}