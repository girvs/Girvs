﻿namespace Girvs.Consul.Infrastructure.ApplicationExtensions;

public static class WebApiApplicationExtensions
{
    public static void UseConsulByWebApi(this IApplicationBuilder app)
    {
        var config = Singleton<AppSettings>.Instance.Get<ConsulConfig>();
           
        if (config.CurrentServerModel == ServerModel.WebApi)
        {
            var lifetime = EngineContext.Current.Resolve<IHostApplicationLifetime>();
            var consulClient =
                new ConsulClient(configuration => configuration.Address = new Uri(config.ConsulAddress));

            config.ServerName = string.IsNullOrEmpty(config.ServerName)
                ? AppDomain.CurrentDomain.FriendlyName.Replace(".", "-").ToLower()
                : config.ServerName;

            var uri = new Uri(config.HealthAddress);

            var registration = new AgentServiceRegistration
            {
                ID = Guid.NewGuid().ToString(),
                Tags = new string[] {".net Core WebApiService"},
                Name = config.ServerName,
                Address = $"{uri.Host}",
                Port = uri.Port,
                Check = new AgentServiceCheck()
                {
                    DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(config.DeregisterCriticalServiceAfter),
                    Interval = TimeSpan.FromSeconds(config.Interval),
                    HTTP = config.HealthAddress,
                    Timeout = TimeSpan.FromSeconds(config.Timeout)
                }
            };

            consulClient.Agent.ServiceRegister(registration).Wait();
            lifetime.ApplicationStopping.Register(() =>
            {
                consulClient.Agent.ServiceDeregister(registration.ID).Wait();
            });
        }
    }
}