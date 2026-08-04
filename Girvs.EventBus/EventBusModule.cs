using DotNetCore.CAP.Internal;
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

        var resources = Singleton<AppSettings>.Instance.Resources;
        var connStr = eventBusConfig.BuildPersistenceConnectionString(
            resources[eventBusConfig.PersistenceConnectionRef]);
        var transport = resources[eventBusConfig.TransportConnectionRef];
        var persistenceType = resources[eventBusConfig.PersistenceConnectionRef].Type.ToLowerInvariant();

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

                switch (transport.Type.ToLowerInvariant())
                {
                    case "rabbitmq":
                        x.UseRabbitMQ(options =>
                        {
                            options.HostName = transport.Settings.GetValueOrDefault("HostName") ??
                                               transport.Settings.GetValueOrDefault("Host");
                            options.Port = int.TryParse(transport.Settings.GetValueOrDefault("Port"), out var port)
                                ? port
                                : 5672;
                            options.UserName = transport.Settings.GetValueOrDefault("UserName");
                            options.Password = transport.Settings.GetValueOrDefault("Password");
                            options.VirtualHost = transport.Settings.GetValueOrDefault("VirtualHost") ?? "/";
                        });
                        break;
                    case "kafka":
                        x.UseKafka(configure =>
                        {
                            configure.Servers = transport.Settings.GetValueOrDefault("BootstrapServers") ??
                                                transport.Settings.GetValueOrDefault("Endpoints");
                            configure.MainConfig.Add(
                                "ssl.ca.location",
                                transport.Settings.GetValueOrDefault("SslCaLocation")
                            );
                            configure.MainConfig.Add(
                                "sasl.mechanism",
                                transport.Settings.GetValueOrDefault("SaslMechanism")
                            );
                            configure.MainConfig.Add(
                                "security.protocol",
                                transport.Settings.GetValueOrDefault("SecurityProtocol")
                            );
                            configure.MainConfig.Add(
                                "sasl.username",
                                transport.Settings.GetValueOrDefault("SaslUsername")
                            );
                            configure.MainConfig.Add(
                                "sasl.password",
                                transport.Settings.GetValueOrDefault("SaslPassword")
                            );
                            //configure.MainConfig.Add("allow.auto.create.topics", "true");
                        });
                        break;
                    case "redis":
                        x.UseRedis(transport.Settings.GetValueOrDefault("Endpoints"));
                        break;
                }

                //x.UseGrivsConfigDataBase();
                x.UseDashboard(d =>
                {
#if DEBUG
                    //d.PathBase = "/cap";
#else
                    d.PathBase = $"/{ServiceNameResolver.FromAssemblyName()}";
#endif
                });
                x.ConsumerThreadCount = eventBusConfig.ConsumerThreadCount;
                x.SucceedMessageExpiredAfter = eventBusConfig.SucceedMessageExpiredAfter;
                x.FailedMessageExpiredAfter = eventBusConfig.FailedMessageExpiredAfter;
                // x.ProducerThreadCount = eventBusConfig.ProducerThreadCount;
            })
            .AddSubscribeFilter<GirvsCapFilter>();
    }

    public void Configure(IApplicationBuilder application, IWebHostEnvironment env)
    {
    }

    public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
    {
    }

    public int Order { get; } = 50000;
}