using Girvs.Configuration;
using Girvs.EventBus.CapEventBus;
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
                            return ((DbType) (int) connectionConfig.UseDataType,
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

        private string GetRedisConnectionString(string eventBusConfigRedisConnStr)
        {
            try
            {
                var cacheConfig = Singleton<AppSettings>.Instance[eventBusConfigRedisConnStr];
                if (cacheConfig.EnableCaching)
                {
                    return cacheConfig.RedisCacheConfig.ConnectionString;
                }
            }
            finally
            {
            }

            return eventBusConfigRedisConnStr;
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

                switch (eventBusConfig.EventBusType)
                {
                    case EventBusType.RabbitMQ:
                        x.UseRabbitMQ(options =>
                        {
                            options.HostName = eventBusConfig.RabbitMqConfig.HostName;
                            options.Port = eventBusConfig.RabbitMqConfig.Port;
                            options.UserName = eventBusConfig.RabbitMqConfig.UserName;
                            options.Password = eventBusConfig.RabbitMqConfig.Password;
                            options.VirtualHost = eventBusConfig.RabbitMqConfig.VirtualHost;
                        });
                        break;
                    case EventBusType.Kafka:
                        x.UseKafka(configure =>
                        {
                            configure.Servers = eventBusConfig.KafkaConfig.KafKaConnectionString;
                            configure.MainConfig.Add("ssl.ca.location", eventBusConfig.KafkaConfig.SslCaLocation);
                            configure.MainConfig.Add("sasl.mechanism", eventBusConfig.KafkaConfig.SaslMechanism);
                            configure.MainConfig.Add("security.protocol", eventBusConfig.KafkaConfig.SecurityProtocol);
                            configure.MainConfig.Add("sasl.username", eventBusConfig.KafkaConfig.SaslUsername);
                            configure.MainConfig.Add("sasl.password", eventBusConfig.KafkaConfig.SaslPassword);
                            //configure.MainConfig.Add("allow.auto.create.topics", "true");
                        });
                        break;
                    case EventBusType.Redis:
                        x.UseRedis(GetRedisConnectionString(eventBusConfig.RedisConfig.RedisConnectionString));
                        break;
                }

                //x.UseGrivsConfigDataBase();
                x.UseDashboard();

                x.ConsumerThreadCount = eventBusConfig.ConsumerThreadCount;
                x.ProducerThreadCount = eventBusConfig.ProducerThreadCount;
            }).AddSubscribeFilter<GirvsCapFilter>();

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
