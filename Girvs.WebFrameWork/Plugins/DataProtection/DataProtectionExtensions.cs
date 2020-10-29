using Girvs.Domain;
using Girvs.Domain.Caching.Interface.Redis;
using Girvs.Domain.Configuration;
using Girvs.Domain.Infrastructure;
using Girvs.Infrastructure.CacheRepository;
using Girvs.Infrastructure.CacheRepository.Redis;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.WebFrameWork.Plugins.DataProtection
{
    public static class DataProtectionExtensions
    {
        /// <summary>
        /// 增加数据保护服务
        /// </summary>
        public static void AddGirvsDataProtection(this IServiceCollection services)
        {
            //检查是否在Redis中保持数据保护
            var config = services.BuildServiceProvider().GetRequiredService<GirvsConfig>();
            if (config.RedisEnabled && config.UseRedisToStoreDataProtectionKeys)
            {
                //将密钥存储在Redis中
                services.AddDataProtection().PersistKeysToStackExchangeRedis(() =>
                {
                    var redisConnectionWrapper = EngineContext.Current.Resolve<IRedisConnectionWrapper>();
                    return redisConnectionWrapper.GetDatabase(config.RedisDatabaseId ?? (int)RedisDatabaseNumber.DataProtectionKeys);
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