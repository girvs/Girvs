using DotNetCore.CAP.Internal;
using Girvs.Configuration.Resources;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Girvs.EventBus;

public class EventBusModule : IAppModuleStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        //Note: The injection of services needs before of `services.AddCap()`
        services.AddCapSubscribe();

        var eventBusConfig = EngineContext.Current.GetAppModuleConfig<EventBusConfig>();

        services.AddScoped<IEventBus, CapEventBus.CapEventBus>();

        // 资源引用为数组：索引 0 为资源键格式串，索引 1 起同时用于组合资源键与格式化 Settings 模板
        var persistenceResource = InfrastructureResourceReferenceResolver.GetRequiredResource(
            eventBusConfig.PersistenceConnectionRef
        );
        var transportResource = InfrastructureResourceReferenceResolver.GetRequiredResource(
            eventBusConfig.TransportConnectionRef
        );

        var connStr = InfrastructureResourceReferenceResolver.GetRequiredSetting(
            eventBusConfig.PersistenceConnectionRef,
            "ConnectionString"
        );
        var persistenceType = (persistenceResource.Type ?? string.Empty).ToLowerInvariant();
        var transportType = (transportResource.Type ?? string.Empty).ToLowerInvariant();

        services
            .AddCap(x =>
            {
                switch (persistenceType)
                {
                    case "sqlserver":
                        x.UseSqlServer(connStr);
                        break;
                    case "mysql":
                        x.UseMySql(connStr);
                        break;
                    // case DbType.Oracle:
                    //     x.UseOracle(connStr);
                    //     break;
                    case "sqlite":
                        x.UseSqlite(connStr);
                        break;
                }

                switch (transportType)
                {
                    case "rabbitmq":
                        x.UseRabbitMQ(options =>
                        {
                            options.ConnectionFactoryOptions = factory =>
                            {
                                factory.Uri = new Uri(
                                    InfrastructureResourceReferenceResolver.GetRequiredSetting(
                                        eventBusConfig.TransportConnectionRef,
                                        "ConnectionString"
                                    )
                                );
                            };
                        });
                        break;
                    case "kafka":
                        x.UseKafka(configure =>
                        {
                            var raw =
                                InfrastructureResourceReferenceResolver.GetRequiredSetting(
                                    eventBusConfig.TransportConnectionRef,
                                    "ConnectionString"
                                );

                            var parts = raw.Split(';', StringSplitOptions.RemoveEmptyEntries);

                            // 第一段是 bootstrap servers
                            configure.Servers = parts[0];

                            // 后续段是 MainConfig 键值对
                            foreach (var part in parts.Skip(1))
                            {
                                var kv = part.Split('=', 2);
                                if (kv.Length == 2)
                                {
                                    configure.MainConfig[kv[0].Trim()] = kv[1].Trim();
                                }
                            }

                            // configure.Servers =
                            //     transport.Settings.GetValueOrDefault("BootstrapServers")
                            //     ?? transport.Settings.GetValueOrDefault("Endpoints");
                            // configure.MainConfig.Add(
                            //     "ssl.ca.location",
                            //     transport.Settings.GetValueOrDefault("SslCaLocation")
                            // );
                            // configure.MainConfig.Add(
                            //     "sasl.mechanism",
                            //     transport.Settings.GetValueOrDefault("SaslMechanism")
                            // );
                            // configure.MainConfig.Add(
                            //     "security.protocol",
                            //     transport.Settings.GetValueOrDefault("SecurityProtocol")
                            // );
                            // configure.MainConfig.Add(
                            //     "sasl.username",
                            //     transport.Settings.GetValueOrDefault("SaslUsername")
                            // );
                            // configure.MainConfig.Add(
                            //     "sasl.password",
                            //     transport.Settings.GetValueOrDefault("SaslPassword")
                            // );
                            //configure.MainConfig.Add("allow.auto.create.topics", "true");
                        });
                        break;
                    case "redis":
                        x.UseRedis(
                            InfrastructureResourceReferenceResolver.GetRequiredSetting(
                                eventBusConfig.TransportConnectionRef,
                                "Endpoints"
                            )
                        );
                        break;
                }

                //x.UseGrivsConfigDataBase();
                if (eventBusConfig.EnableDashboard)
                {
                    // Dashboard 无鉴权，默认关闭；仅显式开启时才挂载路由
                    x.UseDashboard(d =>
                    {
#if DEBUG
                        //d.PathBase = "/cap";
#else
                        d.PathBase = $"/{ServiceNameResolver.FromAssemblyName()}";
#endif
                    });
                }

                x.ConsumerThreadCount = eventBusConfig.ConsumerThreadCount;
                x.SucceedMessageExpiredAfter = eventBusConfig.SucceedMessageExpiredAfter;
                x.FailedMessageExpiredAfter = eventBusConfig.FailedMessageExpiredAfter;
                // x.ProducerThreadCount = eventBusConfig.ProducerThreadCount;
            })
            .AddSubscribeFilter<GirvsCapFilter>();
    }

    public void Configure(IApplicationBuilder application, IWebHostEnvironment env) { }

    public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder) { }

    public int Order { get; } = 50000;
}
