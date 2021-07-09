using Girvs.BusinessBasis.Repositories;
using Girvs.BusinessBasis.UoW;
using Girvs.EntityFrameworkCore.DbContextExtensions;
using Girvs.EntityFrameworkCore.Repositories;
using Girvs.EntityFrameworkCore.UoW;
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
            //services.AddGirvsObjectContext();
            services.AddScoped(typeof(IUnitOfWork<>), typeof(UnitOfWork<>));
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
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