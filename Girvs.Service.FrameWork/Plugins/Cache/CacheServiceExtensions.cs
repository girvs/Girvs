using Girvs.Domain.Caching.Interface;
using Girvs.Domain.Caching.Interface.Redis;
using Girvs.Domain.Configuration;
using Girvs.Infrastructure.CacheRepository;
using Girvs.Infrastructure.CacheRepository.Redis;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.Service.FrameWork.Plugins.Cache
{
    public static class CacheServiceExtensions
    {
        public static void AddEasyCaching(this IServiceCollection services)
        {
            //内存缓存
            services.AddEasyCaching(option =>
            {
                //使用内存缓存
                option.UseInMemory("spCommerce_memory_cache");
            });
        }

        public static void AddCacheService(this IServiceCollection services, GirvsConfig config)
        {
            services.AddSingleton<ICacheManager, PerRequestCacheManager>();
            //services.AddSingleton<ICacheUsingManager, CacheUsingManager>();
            //redis connection wrapper
            if (config.RedisEnabled)
            {
                services.AddSingleton<ILocker, RedisConnectionWrapper>();
                services.AddSingleton<IRedisConnectionWrapper, RedisConnectionWrapper>();
            }

            //static cache manager
            if (config.RedisEnabled && config.UseRedisForCaching)
            {
                services.AddSingleton<IStaticCacheManager, RedisCacheManager>();
            }
            else
            {
                services.AddSingleton<ILocker, MemoryCacheManager>();
                services.AddSingleton<IStaticCacheManager, MemoryCacheManager>();
            }
        }
    }
}