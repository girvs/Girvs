using System;
using System.Linq;
using System.Reflection;
using Girvs.Domain.Configuration;
using Girvs.Domain.Infrastructure;
using Girvs.Domain.Managers;
using Girvs.Domain.TypeFinder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.WebFrameWork.Plugins.Refit
{
    public class RefitStartup : IPluginStartup
    {
        public string Name { get; } = "Refit";

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
            var refitConfigs = config.FunctionalModules
                .FirstOrDefault(d => d.Name == Name)?.Configs;

            var clients = typeFinder.FindClassesOfType<IGirvsHttpClient>(false, true);

            foreach (var client in clients)
            {
                services.AddGirvsRefitClient(client).ConfigureHttpClient(httpClientConfig =>
                {
                    if (refitConfigs != null && refitConfigs.Any())
                    {
                        var attribute = client.GetCustomAttribute<GirvsHttpClientConfigAttribute>();

                        var refitConfig =
                            refitConfigs.FirstOrDefault(c => c.Name == attribute?.ClientName);

                        if (refitConfig == null) return;

                        httpClientConfig.BaseAddress = new Uri(refitConfig.BaseUrl);
                        httpClientConfig.Timeout = TimeSpan.FromMilliseconds(refitConfig.Timeout);
                    }
                });
            }
        }

        public void ConfigureRequestPipeline(IApplicationBuilder application)
        {
        }

        public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
        {
        }

        public int Order { get; } = 110;
    }
}