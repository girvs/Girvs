using Girvs.Cache.Configuration;
using Girvs.Configuration;
using Girvs.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.Cache
{
    public class GirvsCacheModule : IAppModuleStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            var appSettings = Singleton<AppSettings>.Instance;
            var cacheConfig = appSettings.ModuleConfigurations[nameof(CacheConfig)] as CacheConfig;
            
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