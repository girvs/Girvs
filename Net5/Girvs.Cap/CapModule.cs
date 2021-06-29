using System;
using Girvs.Cap.Configuration;
using Girvs.Configuration;
using Girvs.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.Cap
{
    public class CapModule : IAppModuleStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            var appSettings = Singleton<AppSettings>.Instance;
            var capConfig = appSettings.ModuleConfigurations[nameof(CapConfig)] as CapConfig;

            services.AddCap(action =>
            {
                if (capConfig.UseDashboard)
                {
                    action.UseDashboard();
                }

                switch (capConfig.StoreType)
                {
                    case StoreType.SqlServer:
                        action.UseSqlServer(capConfig.DataConnectionString);
                        break;

                    case StoreType.MySql:
                        action.UseMySql(capConfig.DataConnectionString);
                        break;
                    case StoreType.PostgreSql:
                        action.UsePostgreSql(capConfig.DataConnectionString);
                        break;
                    case StoreType.MongoDB:
                        action.UseMongoDB(capConfig.DataConnectionString);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }


                switch (capConfig.MessageMiddleware)
                {
                    case MessageMiddleware.Kafka:
                        action.UseKafka(capConfig.MqConnectionString);
                        break;
                    case MessageMiddleware.RabbitMQ:
                        action.UseRabbitMQ(capConfig.MqConnectionString);
                        break;
                    case MessageMiddleware.AzureServiceBus:
                        action.UseAzureServiceBus(capConfig.MqConnectionString);
                        break;
                    case MessageMiddleware.AmazonSQS:
                        action.UseAmazonSQS(x => { });
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            });
        }

        public void Configure(IApplicationBuilder application)
        {
        }

        public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
        {
        }

        public int Order { get; } = 2;
    }
}
