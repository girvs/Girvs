using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Girvs.Consul;

public class ConsulModule : IAppModuleStartup
{
    private const int ConsulOperationTimeoutSeconds = 5;

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var consulConfig = Singleton<AppSettings>.Instance.Get<ConsulConfig>();

        // Grpc 与 WebApi 两种模式都需要 ConsulClient，统一注册为单例由容器释放，
        // 避免各处 new ConsulClient 后不 Dispose 造成 HttpClient 泄漏。
        services.AddSingleton<IConsulClient>(
            _ =>
                new ConsulClient(consulClientConfig =>
                {
                    consulClientConfig.Address = new Uri(consulConfig.ConsulAddress);
                })
        );
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

        if (
            !consulClient
                .Agent.ServiceRegister(registration)
                .Wait(TimeSpan.FromSeconds(ConsulOperationTimeoutSeconds))
        )
        {
            GetLogger(application)
                ?.LogWarning(
                    "Consul 服务注册超时（{TimeoutSeconds} 秒），注册任务仍在后台继续：服务 {ServerName}，Consul 地址 {ConsulAddress}",
                    ConsulOperationTimeoutSeconds,
                    registration.Name,
                    consulConfig.ConsulAddress
                );
        }

        lifetime.ApplicationStopping.Register(() =>
            Deregister(application, consulClient, registration, consulConfig.ConsulAddress)
        );
    }

    private static void Deregister(
        IApplicationBuilder application,
        IConsulClient consulClient,
        AgentServiceRegistration registration,
        string consulAddress
    )
    {
        try
        {
            if (
                !consulClient
                    .Agent.ServiceDeregister(registration.ID)
                    .Wait(TimeSpan.FromSeconds(ConsulOperationTimeoutSeconds))
            )
            {
                GetLogger(application)
                    ?.LogWarning(
                        "Consul 服务注销超时（{TimeoutSeconds} 秒）：服务 {ServerName}，Consul 地址 {ConsulAddress}",
                        ConsulOperationTimeoutSeconds,
                        registration.Name,
                        consulAddress
                    );
            }
        }
        catch (Exception exception)
        {
            // 停止阶段的回调再抛异常会影响 Host 正常退出，这里只记录
            GetLogger(application)
                ?.LogError(
                    exception,
                    "Consul 服务注销失败：服务 {ServerName}，Consul 地址 {ConsulAddress}",
                    registration.Name,
                    consulAddress
                );
        }
    }

    private static ILogger GetLogger(IApplicationBuilder application) =>
        application
            .ApplicationServices.GetService<ILoggerFactory>()
            ?.CreateLogger(nameof(ConsulModule));

    internal static AgentServiceRegistration CreateGrpcRegistration(ConsulConfig consulConfig)
    {
        consulConfig.ServerName = string.IsNullOrEmpty(consulConfig.ServerName)
            ? ServiceNameResolver.FromAssemblyName()
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
