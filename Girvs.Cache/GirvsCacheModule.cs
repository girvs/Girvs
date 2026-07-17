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

        if (distributedCacheConfig.Enabled && !string.IsNullOrWhiteSpace(distributedCacheConfig.ConnectionRef))
        {
            var resource = Singleton<AppSettings>.Instance.Resources[distributedCacheConfig.ConnectionRef];
            switch (resource.Type.ToLowerInvariant())
            {
                case "sqlserver":
                    var sqlServerConnectionString = GetConnectionString(cacheConfig);
                    services.AddScoped<IStaticCacheManager, MsSqlServerCacheManager>();
                    services.AddScoped<ICacheKeyService, MsSqlServerCacheManager>();
                    services.AddDistributedSqlServerCache(options =>
                    {
                        options.ConnectionString = sqlServerConnectionString;
                        options.SchemaName = distributedCacheConfig.SchemaName;
                        options.TableName = distributedCacheConfig.TableName;
                    });
                    break;

                case "redis":
                    var redisConnectionString = GetConnectionString(cacheConfig);
                    services.AddSingleton<IRedisConnectionWrapper, RedisConnectionWrapper>();
                    services.AddScoped<IStaticCacheManager, RedisCacheManager>();
                    services.AddScoped<ICacheKeyService, RedisCacheManager>();
                    services.AddStackExchangeRedisCache(options =>
                    {
                        options.Configuration = redisConnectionString;
                        options.InstanceName = distributedCacheConfig.InstanceName ?? string.Empty;
                    });
                    break;

                case "redis-synchronized-memory":
                    var synchronizedRedisConnectionString = GetConnectionString(cacheConfig);
                    services.AddSingleton<IRedisConnectionWrapper, RedisConnectionWrapper>();
                    services.AddSingleton<ISynchronizedMemoryCache, RedisSynchronizedMemoryCache>();
                    services.AddSingleton<IStaticCacheManager, SynchronizedMemoryCacheManager>();
                    services.AddScoped<ICacheKeyService, SynchronizedMemoryCacheManager>();
                    services.AddStackExchangeRedisCache(options =>
                    {
                        options.Configuration = synchronizedRedisConnectionString;
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

    private static string GetConnectionString(CacheConfig cacheConfig)
    {
        var cache = cacheConfig.DistributedCacheConfig;
        var resources = Singleton<AppSettings>.Instance.Resources;
        var resource = resources.TryGetValue(cache.ConnectionRef, out var value)
            ? value
            : throw new GirvsException($"Resources:{cache.ConnectionRef} 未配置");
        return cache.BuildConnectionString(resource);
    }
}
