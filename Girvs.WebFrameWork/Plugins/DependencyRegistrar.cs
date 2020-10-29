using Girvs.Domain.Configuration;
using Girvs.Domain.Infrastructure;
using Girvs.Domain.IRepositories;
using Girvs.Domain.Managers;
using Girvs.Domain.TypeFinder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.WebFrameWork.Plugins
{
    public class Plugin : IPluginStartup
    {
        public string DependencyName { get; set; } = "System";

        public string Name { get; } = "system";
        public bool Enabled { get; } = true;

        public void ConfigureServicesRegister(IServiceCollection services, ITypeFinder typeFinder, GirvsConfig config)
        {
            services.RegisterType(typeof(IRepository<>), typeFinder, true);
            services.RegisterType<IManager>(typeFinder, true);
        }

        public void ConfigureRequestPipeline(IApplicationBuilder application)
        {
        }

        public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
        {
        }

        public int Order => 0;
    }
}