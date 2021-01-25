using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Girvs.Domain.Configuration;
using Girvs.Domain.Infrastructure;
using Girvs.Domain.Managers;
using Girvs.Domain.TypeFinder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;

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
            var configuration = EngineContext.Current.Resolve<IConfiguration>();

            var indexOf = config.FunctionalModules.FindIndex(d => d.Name == Name);
            var refitConfigs =
                services.ConfigureStartupConfig<List<RefitConfig>>(
                    configuration.GetSection($"Girvs:FunctionalModules:{indexOf}:RefitConfigs"));

            var clients = typeFinder.FindClassesOfType<IGirvsHttpClient>(false, true);
            
            foreach (var client in clients)
            {
                services.AddRefitClient(client).ConfigureHttpClient(httpClientConfig =>
                {
                    if (refitConfigs.Any())
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