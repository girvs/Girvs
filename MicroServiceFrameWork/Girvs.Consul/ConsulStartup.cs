using System;
using Girvs.Consul.Configuration;
using Girvs.Consul.Infrastructure.ApplicationExtensions;
using Girvs.Domain.Infrastructure;
using Girvs.WebFrameWork.Infrastructure.ServicesExtensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NConsul.AspNetCore;

namespace Girvs.Consul
{
    public class ConsulStartup : IGirvsStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            var consulConfig = services.ConfigureStartupConfig<ConsulConfig>(configuration.GetSection("ConsulConfig"));
            //需要添加判断是否存在GRPC服务
            if (consulConfig.CurrentServerModel == ServerModel.GrpcService)
            {
                consulConfig.ServerName = string.IsNullOrEmpty(consulConfig.ServerName)
                    ? AppDomain.CurrentDomain.FriendlyName.Replace(".", "_")
                    : consulConfig.ServerName;

                var uri = new Uri(consulConfig.HealthAddress);
                services.AddConsul(new NConsulOptions
                    {
                        Address = consulConfig.ConsulAddress,
                    })
                    .AddGRPCHealthCheck(consulConfig.HealthAddress.Replace($"{uri.Scheme}://", ""))
                    .RegisterService(consulConfig.ServerName, uri.Host, uri.Port, new[] {".net Core GrpcService"});
            }
        }

        public void Configure(IApplicationBuilder application)
        {
            //需要添加判断是否存在WebApi服务
            application.UseConsulByWebApi();
        }

        public int Order { get; } = int.MaxValue;
    }
}