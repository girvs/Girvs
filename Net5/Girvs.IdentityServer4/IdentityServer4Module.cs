using System;
using Girvs.Configuration;
using Girvs.IdentityServer4.Configuration;
using Girvs.Infrastructure;
using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.IdentityServer4
{
    public class IdentityServer4Module : IAppModuleStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            var appSettings = Singleton<AppSettings>.Instance;
            var ids4Config = appSettings.ModuleConfigurations[nameof(IdentityServer4Config)] as IdentityServer4Config;
            
            services //.AddAuthorization()
                .AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
                .AddIdentityServerAuthentication(options =>
                {
                    var configuration = EngineContext.Current.Resolve<IConfiguration>();
                    var audienceName = AppDomain.CurrentDomain.FriendlyName.Replace(".", "_");
                    options.Authority = ids4Config.ServerHost;
                    options.RequireHttpsMetadata = ids4Config.UseHttps;
                    options.SupportedTokens = SupportedTokens.Both;
                    options.ApiSecret = ids4Config.ApiSecret;
                    options.ApiName = audienceName;
                });
        }

        public void Configure(IApplicationBuilder application)
        {
            application.UseAuthentication();
            application.UseAuthorization();
        }

        public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
        {
            
        }

        public int Order { get; }
    }
}