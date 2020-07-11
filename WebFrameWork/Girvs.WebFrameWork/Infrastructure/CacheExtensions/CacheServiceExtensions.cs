﻿using Girvs.Domain;
using Girvs.Domain.Caching;
using Girvs.Domain.Caching.Redis;
using Girvs.Domain.Configuration;
using Girvs.Domain.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.WebFrameWork.Infrastructure.CacheExtensions
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
            services.AddSingleton<ICacheUsingManager, CacheUsingManager>();
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

        /// <summary>
        /// 增加数据保护服务
        /// </summary>
        public static void AddSpDataProtection(this IServiceCollection services)
        {
            //检查是否在Redis中保持数据保护
            var spConfig = services.BuildServiceProvider().GetRequiredService<GirvsConfig>();
            if (spConfig.RedisEnabled && spConfig.UseRedisToStoreDataProtectionKeys)
            {
                //将密钥存储在Redis中
                services.AddDataProtection().PersistKeysToStackExchangeRedis(() =>
                {
                    var redisConnectionWrapper = EngineContext.Current.Resolve<IRedisConnectionWrapper>();
                    return redisConnectionWrapper.GetDatabase(spConfig.RedisDatabaseId ?? (int)RedisDatabaseNumber.DataProtectionKeys);
                }, GirvsCachingDefaults.RedisDataProtectionKey);
            }
            else
            {
                var dataProtectionKeysPath = CommonHelper.DefaultFileProvider.MapPath("~/App_Data/DataProtectionKeys");
                var dataProtectionKeysFolder = new System.IO.DirectoryInfo(dataProtectionKeysPath);

                //配置数据保护系统以将密钥保留到指定目录
                services.AddDataProtection().PersistKeysToFileSystem(dataProtectionKeysFolder);
            }
        }
    }
}