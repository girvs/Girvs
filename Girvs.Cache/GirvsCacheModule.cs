using Girvs.Cache.CacheImps;
using Girvs.Cache.Caching;
using Girvs.Configuration.Resources;
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

        if (
            distributedCacheConfig.Enabled
            && InfrastructureResourceReferenceResolver.IsConfigured(
                distributedCacheConfig.ConnectionRef
            )
        )
        {
            var resource = InfrastructureResourceReferenceResolver.GetRequiredResource(
                distributedCacheConfig.ConnectionRef
            );

            switch ((resource.Type ?? string.Empty).ToLowerInvariant())
            {
                // case "sqlserver":
                //     var sqlServerConnectionString = GetConnectionString(cacheConfig);
                //     services.AddScoped<IStaticCacheManager, MsSqlServerCacheManager>();
                //     services.AddScoped<ICacheKeyService, MsSqlServerCacheManager>();
                //     services.AddDistributedSqlServerCache(options =>
                //     {
                //         options.ConnectionString = sqlServerConnectionString;
                //         options.SchemaName = distributedCacheConfig.SchemaName;
                //         options.TableName = distributedCacheConfig.TableName;
                //     });
                //     break;

                case "redis":
                    var redisConnectionString =
                        InfrastructureResourceReferenceResolver.GetRequiredSetting(
                            distributedCacheConfig.ConnectionRef,
                            "Endpoints"
                        );
                    services.AddSingleton<RedisConnectionWrapper>();
                    services.AddSingleton<IRedisConnectionWrapper>(sp =>
                        sp.GetRequiredService<RedisConnectionWrapper>()
                    );
                    services.AddScoped<IStaticCacheManager, RedisCacheManager>();
                    services.AddScoped<ICacheKeyService, RedisCacheManager>();
                    services.AddStackExchangeRedisCache(options =>
                    {
                        options.Configuration = redisConnectionString;
                        options.InstanceName = distributedCacheConfig.InstanceName ?? string.Empty;
                    });
                    break;

                case "redis-synchronized-memory":
                    var synchronizedRedisConnectionString =
                        InfrastructureResourceReferenceResolver.GetRequiredSetting(
                            distributedCacheConfig.ConnectionRef,
                            "Endpoints"
                        );
                    // RedisSynchronizedMemoryCache 以本地内存缓存为主存储、Redis 仅做失效同步,必须注册 IMemoryCache
                    services.AddSingleton<IMemoryCache>(new MemoryCache(new MemoryCacheOptions()));
                    services.AddSingleton<RedisConnectionWrapper>();
                    services.AddSingleton<IRedisConnectionWrapper>(sp =>
                        sp.GetRequiredService<RedisConnectionWrapper>()
                    );
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
}
