using Girvs.Domain.Configuration;
using Girvs.Domain.Infrastructure;
using Girvs.Domain.TypeFinder;
using Girvs.IdentityServer4.Configuration;
using Girvs.WebFrameWork.Plugins;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
                services.ConfigureStartupConfig<IdentityServer4Config>(configuration.GetSection("IdentityServer4"));
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.Authority = idsConfig.ServerHost;
                    options.RequireHttpsMetadata = false;
                    options.Audience = idsConfig.ClientName;
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