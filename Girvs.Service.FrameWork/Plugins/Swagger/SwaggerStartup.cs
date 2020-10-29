using System.Linq;
using Girvs.Domain.Configuration;
using Girvs.Domain.Infrastructure;
using Girvs.Domain.TypeFinder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.Service.FrameWork.Plugins.Swagger
{
    public class SwaggerStartup : IPluginStartup
    {
        public string Name { get; }= "Swagger";
        public bool Enabled
        {
            get
            {
                var config = EngineContext.Current.Resolve<GirvsConfig>();
                var funcConfig = config.FunctionalModules.SingleOrDefault(x => x.Name == Name);
                return funcConfig != null && funcConfig.Enabled;
            }
        }
        public void ConfigureServicesRegister(IServiceCollection services, ITypeFinder typeFinder, GirvsConfig config)
        {
            services.AddSwaggerServices();
        }

        public void ConfigureRequestPipeline(IApplicationBuilder application)
        {
            application.UseSwaggerService();
        }

        public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
        {
        }

        public int Order { get; } = 202;
    }
}