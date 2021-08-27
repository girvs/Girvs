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
        private (DbType, string) GetDbConnString(EventBusConfig eventBusConfig)
        {
            try
            {
                var dbConfig = Singleton<AppSettings>.Instance["DbConfig"];

                if (dbConfig != null)
                {
                    foreach (var connectionConfig in dbConfig.DataConnectionConfigs)
                    {
                        if (connectionConfig.Name == eventBusConfig.DbConnectionString)
                        {
                            return ((DbType)(int)connectionConfig.UseDataType,
                                connectionConfig.MasterDataConnectionString);
                        }
                    }
                }
            }
            finally
            {
                
            }
            return (eventBusConfig.DbType, eventBusConfig.DbConnectionString);
        }

        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            var eventBusConfig = EngineContext.Current.GetAppModuleConfig<EventBusConfig>();

            services.AddSingleton<IEventBus, CapEventBus.CapEventBus>();

            var (dbType, connStr) = GetDbConnString(eventBusConfig);            

            services.AddCap(x =>
            {
                switch (dbType)
                {
                    case DbType.MsSql:
                        x.UseSqlServer(connStr);
                        break;
                    case DbType.MySql:
                        x.UseMySql(connStr);
                        break;
                    case DbType.Oracle:
                        x.UseOracle(connStr);
                        break;
                    case DbType.SqlLite:
                        x.UseSqlite(connStr);
                        break;
                }

                x.UseRabbitMQ(options =>
                {
                    options.HostName = eventBusConfig.HostName;
                    options.Port = eventBusConfig.Port;
                    options.UserName = eventBusConfig.UserName;
                    options.Password = eventBusConfig.Password;
                    options.VirtualHost = eventBusConfig.VirtualHost;
                });
                //x.UseGrivsConfigDataBase();
                x.UseDashboard();

                x.ConsumerThreadCount = 1;
                x.ProducerThreadCount = 1;
            });

            services.AddCapSubscribe();
        }

        public void Configure(IApplicationBuilder application)
        {
        }

        public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
        {
        }

        public int Order { get; } = 50000;
    }
}