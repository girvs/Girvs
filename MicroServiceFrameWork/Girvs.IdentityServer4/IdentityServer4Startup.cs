using System;
using Girvs.Domain.Configuration;
using Girvs.Domain.Infrastructure;
using Girvs.Domain.TypeFinder;
using IdentityServer4.AccessTokenValidation;
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
            services //.AddAuthorization()
                .AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
                .AddIdentityServerAuthentication(options =>
                {
                    var configuration = EngineContext.Current.Resolve<IConfiguration>();
                    var audienceName = AppDomain.CurrentDomain.FriendlyName.Replace(".", "_");
                    options.Authority = configuration["IdentityServer:Uri"];
                    options.RequireHttpsMetadata = bool.Parse(configuration["IdentityServer:UseHttps"]);
                    options.SupportedTokens = SupportedTokens.Both;
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