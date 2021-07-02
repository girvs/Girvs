using Girvs.Configuration;
using Girvs.EntityFrameworkCore.Configuration;
using Girvs.EntityFrameworkCore.DbContextExtensions;
using Girvs.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.EntityFrameworkCore
{
    public class GirvsEntityFrameworkCoreModule : IAppModuleStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddGirvsObjectContext();
        }

        public void Configure(IApplicationBuilder application)
        {
        }

        public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
        {
        }

        public int Order { get; } = 5;
    }
}