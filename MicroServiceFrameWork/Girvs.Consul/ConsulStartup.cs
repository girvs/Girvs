using System;
using Girvs.Consul.Configuration;
using Girvs.Consul.Infrastructure.ApplicationExtensions;
using Girvs.Domain.Configuration;
using Girvs.Domain.Infrastructure;
using Girvs.Domain.TypeFinder;
using Girvs.WebFrameWork.Infrastructure.ServicesExtensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NConsul.AspNetCore;

namespace Girvs.Consul
{
    public class ConsulStartup : IPluginStartup
    {
        public string Name { get; } = "Consul";
        public bool Enabled { get; } = true;
        public void ConfigureServicesRegister(IServiceCollection services, ITypeFinder typeFinder, GirvsConfig config)
        {
            var configuration = EngineContext.Current.Resolve<IConfiguration>();
            var girvsConfig = EngineContext.Current.Resolve<GirvsConfig>();
            var consulConfig = services.ConfigureStartupConfig<ConsulConfig>(configuration.GetSection("ConsulConfig"));
            //需要添加判断是否存在GRPC服务
            if (girvsConfig.CurrentServerModel == ServerModel.GrpcService)
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

        public void ConfigureRequestPipeline(IApplicationBuilder application)
        {
            //需要添加判断是否存在WebApi服务
            application.UseConsulByWebApi();
        }

        public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
        {
        }


        public int Order { get; } = int.MaxValue;
    }
}