using Girvs.Cache.Configuration;
using Girvs.Configuration;
using Girvs.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Girvs.Cache.Memory;
using Microsoft.Extensions.Caching.Memory;

namespace Girvs.Cache
{
    public class GirvsCacheModule : IAppModuleStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            var appSettings = Singleton<AppSettings>.Instance;
            var cacheConfig = appSettings.ModuleConfigurations[nameof(CacheConfig)] as CacheConfig;

            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            services.AddSingleton<IMemoryCache>(memoryCache);
            services.AddSingleton<IStaticCacheManager, MemoryCacheManager>();
            services.AddDistributedMemoryCache();

        }

        public void Configure(IApplicationBuilder application)
        {

        }

        public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
        {

        }

        public int Order { get; } = 1;
    }
}