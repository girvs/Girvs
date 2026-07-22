using Microsoft.AspNetCore.Hosting;

namespace Girvs.Consul;

public class ConsulModule : IAppModuleStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var consulConfig = Singleton<AppSettings>.Instance.Get<ConsulConfig>();

        if (consulConfig.CurrentServerModel == ServerModel.GrpcService)
        {
            services.AddSingleton<IConsulClient>(
                _ =>
                    new ConsulClient(consulClientConfig =>
                    {
                        consulClientConfig.Address = new Uri(consulConfig.ConsulAddress);
                    })
            );
        }
    }

    public void Configure(IApplicationBuilder application, IWebHostEnvironment env)
    {
        RegisterGrpcServiceIfNeeded(application);
        application.UseConsulByWebApi();
    }

    public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder) { }

    public int Order { get; } = 99999;

    private static void RegisterGrpcServiceIfNeeded(IApplicationBuilder application)
    {
        var consulConfig = Singleton<AppSettings>.Instance.Get<ConsulConfig>();
        if (consulConfig.CurrentServerModel != ServerModel.GrpcService)
        {
            return;
        }

        var lifetime = EngineContext.Current.Resolve<IHostApplicationLifetime>();
        var consulClient = application.ApplicationServices.GetRequiredService<IConsulClient>();
        var registration = CreateGrpcRegistration(consulConfig);

        consulClient.Agent.ServiceRegister(registration).GetAwaiter().GetResult();
        lifetime.ApplicationStopping.Register(() =>
        {
            consulClient.Agent.ServiceDeregister(registration.ID).GetAwaiter().GetResult();
        });
    }

    internal static AgentServiceRegistration CreateGrpcRegistration(ConsulConfig consulConfig)
    {
        consulConfig.ServerName = string.IsNullOrEmpty(consulConfig.ServerName)
            ? ServiceNameResolver.FromAssemblyName(AppDomain.CurrentDomain.FriendlyName)
            : consulConfig.ServerName;

        var uri = new Uri(consulConfig.HealthAddress);
        return new AgentServiceRegistration
        {
            ID = Guid.NewGuid().ToString(),
            Tags = new[] { ".net Core GrpcService" },
            Name = consulConfig.ServerName,
            Address = uri.Host,
            Port = uri.Port,
            Check = new AgentServiceCheck
            {
                DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(
                    consulConfig.DeregisterCriticalServiceAfter
                ),
                Interval = TimeSpan.FromSeconds(consulConfig.Interval),
                GRPC = consulConfig.HealthAddress.Replace($"{uri.Scheme}://", ""),
                Timeout = TimeSpan.FromSeconds(consulConfig.Timeout)
            }
        };
    }
}
