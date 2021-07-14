using System;
using Girvs.Configuration;
using Girvs.EventBus.Configuration;
using Girvs.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.EventBus
{
    public class EventBusModule : IAppModuleStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            var eventBusConfig =
                Singleton<AppSettings>.Instance.ModuleConfigurations[nameof(EventBusConfig)] as EventBusConfig;

            services.AddCap(x =>
            {
                switch (eventBusConfig.DbType)
                {
                    case DbType.MsSql:
                        x.UseSqlServer(eventBusConfig.DbConnectionString);
                        break;
                    case DbType.MySql:
                        x.UseMySql(eventBusConfig.DbConnectionString);
                        break;
                    case DbType.Oracle:
                        x.UseOracle(eventBusConfig.DbConnectionString);
                        break;
                    case DbType.SqlLite:
                        x.UseSqlite(eventBusConfig.DbConnectionString);
                        break;
                }

                x.UseRabbitMQ(eventBusConfig.RabbitMqConnectionString);
                //x.UseGrivsConfigDataBase();
                x.UseDashboard();
            });
        }

        public void Configure(IApplicationBuilder application)
        {
        }

        public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
        {
        }

        public int Order { get; } = 13;
    }
}