using System;
using Girvs.Configuration;
using Girvs.Infrastructure;
using Girvs.Quartz.Configuration;
using Girvs.WebFrameWork.Plugins.Quartz;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.Quartz
{
    public class QuartzModule : IAppModuleStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            var appSettings = Singleton<AppSettings>.Instance;
            var quartzConfig = appSettings.ModuleConfigurations[nameof(QuartzConfiguration)] as QuartzConfiguration;
            services.AddQuartzHosted(quartzConfig);
        }

        public void Configure(IApplicationBuilder application)
        {
        }

        public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
        {
        }

        public int Order { get; }
    }
}