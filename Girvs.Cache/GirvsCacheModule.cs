using Microsoft.AspNetCore.Hosting;

namespace Girvs.Cache;

public class GirvsCacheModule : IAppModuleStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var cacheConfig = Singleton<AppSettings>.Instance.Get<CacheConfig>();

        switch (cacheConfig.DistributedCacheType)
        {
            case CacheType.Memory:
                services.AddScoped<ILocker, MemoryCacheManager>();
                services.AddSingleton<IStaticCacheManager, MemoryCacheManager>();
                break;
            case CacheType.Redis:
                services.AddSingleton<IRedisConnectionWrapper, RedisConnectionWrapper>();
                services.AddScoped<ILocker, RedisConnectionWrapper>();
                services.AddSingleton<IStaticCacheManager, RedisCacheManager>();
                break;
        }
    }

    public void Configure(IApplicationBuilder application, IWebHostEnvironment env) { }

    public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder) { }

    public int Order { get; } = 1;
}
