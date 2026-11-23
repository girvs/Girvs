using Girvs.Cache.CacheImps;
using Girvs.Cache.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;

namespace Girvs.Cache.Extensions;

public static class GirvsDistributedCacheExtension
{
    // 字符串重载直接传递 KEYS/ARGV，StackExchange.Redis 按节点缓存脚本 SHA1。
    private static readonly string _setScript = @"
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

                    return 1";

    // 首次读取失败时允许后续写入重试。
    private static readonly Lazy<string> _instanceKey = new(
        () => EngineContext.Current.GetAppModuleConfig<CacheConfig>().DistributedCacheConfig.InstanceName
            ?? string.Empty,
        LazyThreadSafetyMode.PublicationOnly
    );

    private static volatile IRedisConnectionWrapper? _redisConnectionWrapper;

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
                var redisConnectionWrapper = _redisConnectionWrapper ??=
                    EngineContext.Current.Resolve<IRedisConnectionWrapper>();
                var db = await redisConnectionWrapper.GetDatabaseAsync();

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

                var result = (int) await db.ScriptEvaluateAsync(_setScript,
                    [$"{_instanceKey.Value}{key}"],
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
