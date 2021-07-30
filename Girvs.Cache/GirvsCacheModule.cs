using Girvs.Cache.Caching;
using Girvs.Cache.Configuration;
using Girvs.Cache.Redis;
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

            switch (cacheConfig.DistributedCacheType)
            {
                case CacheType.Memory:
                    services.AddSingleton<ILocker, MemoryCacheManager>();
                    services.AddSingleton<IStaticCacheManager, MemoryCacheManager>();
                    break;
                case CacheType.Redis:
                    services.AddSingleton<IRedisConnectionWrapper, RedisConnectionWrapper>();
                    services.AddSingleton<ILocker, RedisConnectionWrapper>();
                    services.AddSingleton<IStaticCacheManager, RedisCacheManager>();
                    break;
            }
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