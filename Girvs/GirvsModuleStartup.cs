using Girvs.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Girvs.Extensions;
using LogDashboard;
using Microsoft.AspNetCore.HttpOverrides;

namespace Girvs
{
    public class GirvsModuleStartup : IAppModuleStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.RegisterRepository();
            services.RegisterUow();
            services.RegisterManager();
            services.AddLogDashboard();
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });
        }

        public void Configure(IApplicationBuilder application)
        {
            application.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });
            application.UseLogDashboard();
        }

        public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
        {
            
        }

        public int Order { get; } = 0;
    }
}