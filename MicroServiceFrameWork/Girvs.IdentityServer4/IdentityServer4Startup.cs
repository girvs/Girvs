using Girvs.Domain.Infrastructure;
using Girvs.IdentityServer4.Configuration;
using Girvs.WebFrameWork.Infrastructure.ServicesExtensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.IdentityServer4
{
    public class IdentityServer4Startup : IGirvsStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
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

        public void Configure(IApplicationBuilder application)
        {
            application.UseAuthentication();
            application.UseAuthorization();
        }

        public void EndpointRouteBuilder(IEndpointRouteBuilder builder)
        {
            
        }

        public int Order { get; } = 9;
    }
}