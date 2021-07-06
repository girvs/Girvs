using Girvs.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Girvs.Extensions;

namespace Girvs
{
    public class GirvsModuleStartup : IAppModuleStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.RegisterRepository();
            services.RegisterUow();
            services.RegisterManager();
        }

        public void Configure(IApplicationBuilder application)
        {
            
        }

        public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
        {
            
        }

        public int Order { get; } = 0;
    }
}