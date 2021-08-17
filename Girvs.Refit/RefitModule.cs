using System;
using System.Linq;
using System.Reflection;
using Girvs.Infrastructure;
using Girvs.Refit.HttpClientHandlers;
using Girvs.TypeFinder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace Girvs.Refit
{
    public class RefitModule : IAppModuleStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            var typeFinder = new WebAppTypeFinder();

            var refits = typeFinder.FindOfType<IGirvsRefit>(findType: FindType.Interface)
                .Where(x => x.Name != nameof(IGirvsRefit));

            foreach (var refit in refits)
            {
                if (refit.GetCustomAttribute(typeof(RefitServiceAttribute)) is RefitServiceAttribute refitService)
                {
                    services.AddRefitClient(refit, new RefitSettings(new SystemTextJsonContentSerializer()))
                        // //设置服务名称，andc-api-sys是系统在Consul注册的服务名
                        .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://localhost:5000"))
                        .AddHttpMessageHandler(() => new AuthenticatedHttpClientHandler(refitService));
                }
            }
        }

        public void Configure(IApplicationBuilder application)
        {
        }

        public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
        {
        }

        public int Order { get; }
    }
}