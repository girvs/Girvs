using Girvs.Cache.CacheImps;
using Girvs.Cache.Caching;
using Microsoft.AspNetCore.Hosting;

namespace Girvs.Cache;

public class GirvsCacheModule : IAppModuleStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var cacheConfig = Singleton<AppSettings>.Instance.Get<CacheConfig>();
        var distributedCacheConfig = cacheConfig.DistributedCacheConfig;

        services.AddTransient(typeof(IConcurrentCollection<>), typeof(ConcurrentTrie<>));

        services.AddSingleton<ICacheKeyManager, CacheKeyManager>();
        services.AddScoped<IShortTermCacheManager, PerRequestCacheManager>();

        if (distributedCacheConfig.Enabled)
        {
            switch (distributedCacheConfig.DistributedCacheType)
            {
                case DistributedCacheType.Memory:
                    services.AddDistributedMemoryCache();
                    services.AddScoped<IStaticCacheManager, MemoryDistributedCacheManager>();
                    services.AddScoped<ICacheKeyService, MemoryDistributedCacheManager>();
                    break;

                case DistributedCacheType.SqlServer:
                    services.AddScoped<IStaticCacheManager, MsSqlServerCacheManager>();
                    services.AddScoped<ICacheKeyService, MsSqlServerCacheManager>();
                    services.AddDistributedSqlServerCache(options =>
                    {
                        options.ConnectionString = distributedCacheConfig.ConnectionString;
                        options.SchemaName = distributedCacheConfig.SchemaName;
                        options.TableName = distributedCacheConfig.TableName;
                    });
                    break;

                case DistributedCacheType.Redis:
                    services.AddSingleton<IRedisConnectionWrapper, RedisConnectionWrapper>();
                    services.AddScoped<IStaticCacheManager, RedisCacheManager>();
                    services.AddScoped<ICacheKeyService, RedisCacheManager>();
                    services.AddStackExchangeRedisCache(options =>
                    {
                        options.Configuration = distributedCacheConfig.ConnectionString;
                        options.InstanceName = distributedCacheConfig.InstanceName ?? string.Empty;
                    });
                    break;

                case DistributedCacheType.RedisSynchronizedMemory:
                    services.AddSingleton<IRedisConnectionWrapper, RedisConnectionWrapper>();
                    services.AddSingleton<ISynchronizedMemoryCache, RedisSynchronizedMemoryCache>();
                    services.AddSingleton<IStaticCacheManager, SynchronizedMemoryCacheManager>();
                    services.AddScoped<ICacheKeyService, SynchronizedMemoryCacheManager>();
                    services.AddStackExchangeRedisCache(options =>
                    {
                        options.Configuration = distributedCacheConfig.ConnectionString;
                        options.InstanceName = distributedCacheConfig.InstanceName ?? string.Empty;
                    });
                    break;
            }
            services.AddSingleton<ILocker, DistributedCacheLocker>();
        }
        else
        {
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            services.AddSingleton<IMemoryCache>(memoryCache);
            services.AddSingleton<ILocker, MemoryCacheLocker>();
            services.AddSingleton<IStaticCacheManager, MemoryCacheManager>();
            services.AddScoped<ICacheKeyService, MemoryCacheManager>();
        }
    }

    public void Configure(IApplicationBuilder application, IWebHostEnvironment env) { }

    public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder) { }

    public int Order { get; } = 1;
}
