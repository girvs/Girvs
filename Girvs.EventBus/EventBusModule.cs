using Girvs.Configuration;
using Girvs.EventBus.Configuration;
using Girvs.EventBus.Extensions;
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

            services.AddSingleton<IEventBus, CapEventBus.CapEventBus>();

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

                x.UseRabbitMQ(options =>
                {
                    options.HostName = eventBusConfig.HostName;
                    options.Port = eventBusConfig.Port;
                    options.UserName = eventBusConfig.UserName;
                    options.Password = eventBusConfig.Password;
                    options.VirtualHost = eventBusConfig.VirtualHost;
                    //options.ExchangeName = eventBusConfig.ExchangeName;
                });
                //x.UseGrivsConfigDataBase();
                x.UseDashboard();
            });
            
            services.AddCapSubscribe();
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