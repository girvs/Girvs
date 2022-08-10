namespace Girvs.Cache;

public class GirvsCacheModule : IAppModuleStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var cacheConfig = Singleton<AppSettings>.Instance.Get<CacheConfig>();

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