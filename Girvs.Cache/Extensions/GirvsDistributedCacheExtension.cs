using Girvs.Cache.CacheImps;
using Girvs.Cache.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;

namespace Girvs.Cache.Extensions;

public static class GirvsDistributedCacheExtension
{
    // private const string RedisCallCommandKey = "HSET";
    // private const string AbsoluteExpirationKey = "absexp";
    // private const string SlidingExpirationKey = "sldexp";
    // private const string DataKey = "data";


    private static string GetInstanceKey()
    {
        var cacheConfig = EngineContext.Current.GetAppModuleConfig<CacheConfig>();
        return cacheConfig.DistributedCacheConfig.InstanceName;
    }

    public static async Task<bool> SafeClusterSetAsync(
        this IDistributedCache cache,
        string key,
        string value,
        GirvsDistributedCacheEntryOptions options
    )
    {
        if (cache is RedisCache)
        {
            try
            {
                var redisConnectionWrapper = EngineContext.Current.Resolve<IRedisConnectionWrapper>();
                var db = await redisConnectionWrapper.GetDatabaseAsync();

                var script = @"
                    -- 检查NX条件
                    if ARGV[5] == '1' and redis.call('EXISTS', KEYS[1]) == 1 then
                        return 0
                    end
                    
                    -- 设置数据值
                    redis.call('HSET', KEYS[1], 'data', ARGV[1])
                    
                    -- 处理绝对过期时间
                    if ARGV[2] ~= '' then
                        redis.call('HSET', KEYS[1], 'absexp', ARGV[2])
                    else
                        redis.call('HDEL', KEYS[1], 'absexp')
                    end
                    
                    -- 处理滑动过期时间
                    if ARGV[3] ~= '' then
                        redis.call('HSET', KEYS[1], 'sldexp', ARGV[3])
                    else
                        redis.call('HDEL', KEYS[1], 'sldexp')
                    end
                    
                    -- 设置实际过期时间
                    if ARGV[4] ~= '' then
                        redis.call('EXPIRE', KEYS[1], ARGV[4])
                    end
                    
                    return 1"; // 返回1表示成功

                // 计算实际TTL（优先使用绝对过期，其次滑动过期）
                long? ttlSeconds = null;
                if (options.AbsoluteExpiration.HasValue)
                {
                    ttlSeconds = (long) (options.AbsoluteExpiration.Value - DateTimeOffset.UtcNow).TotalSeconds;
                }
                else if (options.AbsoluteExpirationRelativeToNow.HasValue)
                {
                    ttlSeconds = (long) options.AbsoluteExpirationRelativeToNow.Value.TotalSeconds;
                }
                else if (options.SlidingExpiration.HasValue)
                {
                    ttlSeconds = (long) options.SlidingExpiration.Value.TotalSeconds;
                }

                var instanceKey = GetInstanceKey();

                var result = (int) await db.ScriptEvaluateAsync(script,
                    [$"{instanceKey}{key}"],
                    [
                        value,
                        options.AbsoluteExpiration?.Ticks.ToString() ?? "",
                        options.SlidingExpiration?.Ticks.ToString() ?? "",
                        ttlSeconds?.ToString() ?? "",
                        options.When == When.NotExists ? "1" : "0" // NX条件标志
                    ]);
                return result == 1;
            }
            catch
            {
                // 捕获所有异常（连接问题、脚本错误等）
                return false;
            }
        }
        else
        {
            await cache.SetStringAsync(
                key,
                value,
                options
            );

            return true;
        }
    }
}