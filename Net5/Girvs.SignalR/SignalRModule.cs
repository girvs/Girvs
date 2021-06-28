using System;
using Girvs.Infrastructure;
using Girvs.WebFrameWork.Plugins.SignalR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.SignalR
{
    public class SignalRModule: IAppModuleStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSignalRCore();
        }

        public void Configure(IApplicationBuilder application)
        {
            
        }

        public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
        {
            builder.AutoMapSignalREndpointRouteBuilder();
        }

        public int Order { get; }
    }
}