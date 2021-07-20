using Girvs.AuthorizePermission.Configuration;
using Girvs.AuthorizePermission.Extensions;
using Girvs.Configuration;
using Girvs.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.AuthorizePermission
{
    public class GirvsAuthorizeModule : IAppModuleStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddJwtBearerAuthentication();
        }

        public void Configure(IApplicationBuilder application)
        {
            var claimConfig = Singleton<AppSettings>.Instance[nameof(ClaimValueConfig)] as ClaimValueConfig;

            if (claimConfig.EnableGirvsAuthorize)
            {
                application.UseAuthentication();
                application.UseAuthorization();
            }
        }

        public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
        {
            
        }

        public int Order { get; } = 99905;
    }
}