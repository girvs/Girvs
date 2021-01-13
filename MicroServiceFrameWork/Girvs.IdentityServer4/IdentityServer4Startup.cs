using System;
using Girvs.Domain.Configuration;
using Girvs.Domain.Infrastructure;
using Girvs.Domain.TypeFinder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.IdentityServer4
{
    public class IdentityServer4Startup : IPluginStartup
    {
        public string Name { get; } = "IdentityServer4";
        public bool Enabled { get; } = true;

        public void ConfigureServicesRegister(IServiceCollection services, ITypeFinder typeFinder, GirvsConfig config)
        {
            var configuration = EngineContext.Current.Resolve<IConfiguration>();
            var idsConfig =
                services.AddAuthorization().AddAuthentication("Bearer").AddIdentityServerAuthentication(options =>
                {
                    var audienceName = AppDomain.CurrentDomain.FriendlyName.Replace(".", "_");
                    options.RequireHttpsMetadata = bool.Parse(configuration["IdentityServer:UseHttps"]);
                    options.Authority = configuration["IdentityServer:Uri"];
                    options.ApiSecret = configuration["IdentityServer:ApiSecret"];
                    options.ApiName = audienceName;
                });
        }

        public void ConfigureRequestPipeline(IApplicationBuilder application)
        {
        }

        public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
        {
        }

        public int Order { get; } = 9;
    }
}