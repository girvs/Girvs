using Microsoft.Extensions.Logging;

namespace Girvs.Consul.Infrastructure.ApplicationExtensions;

public static class WebApiApplicationExtensions
{
    private const int ConsulOperationTimeoutSeconds = 5;

    public static void UseConsulByWebApi(this IApplicationBuilder app)
    {
        var config = Singleton<AppSettings>.Instance.Get<ConsulConfig>();

        if (config.CurrentServerModel == ServerModel.WebApi)
        {
            var lifetime = EngineContext.Current.Resolve<IHostApplicationLifetime>();
            // 复用容器注册的单例，避免每次 new ConsulClient 后不 Dispose 造成泄漏
            var consulClient = app.ApplicationServices.GetRequiredService<IConsulClient>();

            config.ServerName = string.IsNullOrEmpty(config.ServerName)
                ? ServiceNameResolver.FromAssemblyName()
                : config.ServerName;

            var uri = new Uri(config.HealthAddress);

            var registration = new AgentServiceRegistration
            {
                ID = Guid.NewGuid().ToString(),
                Tags = new[] { ".net Core WebApiService" },
                Name = config.ServerName,
                Address = $"{uri.Host}",
                Port = uri.Port,
                Check = new AgentServiceCheck
                {
                    DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(
                        config.DeregisterCriticalServiceAfter
                    ),
                    Interval = TimeSpan.FromSeconds(config.Interval),
                    HTTP = config.HealthAddress,
                    Timeout = TimeSpan.FromSeconds(config.Timeout)
                }
            };

            if (
                !consulClient
                    .Agent.ServiceRegister(registration)
                    .Wait(TimeSpan.FromSeconds(ConsulOperationTimeoutSeconds))
            )
            {
                GetLogger(app)
                    ?.LogWarning(
                        "Consul 服务注册超时（{TimeoutSeconds} 秒），注册任务仍在后台继续：服务 {ServerName}，Consul 地址 {ConsulAddress}",
                        ConsulOperationTimeoutSeconds,
                        registration.Name,
                        config.ConsulAddress
                    );
            }

            lifetime.ApplicationStopping.Register(() =>
                Deregister(app, consulClient, registration, config.ConsulAddress)
            );
        }
    }

    private static void Deregister(
        IApplicationBuilder app,
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
                GetLogger(app)
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
            GetLogger(app)
                ?.LogError(
                    exception,
                    "Consul 服务注销失败：服务 {ServerName}，Consul 地址 {ConsulAddress}",
                    registration.Name,
                    consulAddress
                );
        }
    }

    private static ILogger GetLogger(IApplicationBuilder app) =>
        app.ApplicationServices.GetService<ILoggerFactory>()?.CreateLogger("Girvs.Consul");
}
