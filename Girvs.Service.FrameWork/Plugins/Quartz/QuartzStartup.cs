using System.Linq;
using Girvs.Domain.Configuration;
using Girvs.Domain.Infrastructure;
using Girvs.Domain.TypeFinder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.Service.FrameWork.Plugins.Quartz
{
    public class QuartzStartup : IPluginStartup
    {
        public string Name { get; } = "Quartz";

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
            services.AddQuartzHosted();
        }

        public void ConfigureRequestPipeline(IApplicationBuilder application)
        {
        }

        public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
        {
        }

        public int Order { get; } = 105;
    }
}