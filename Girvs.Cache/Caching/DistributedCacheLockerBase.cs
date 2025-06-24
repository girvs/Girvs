using Girvs.Cache.CacheImps;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;

namespace Girvs.Cache.Caching;

public abstract class DistributedCacheLockerBase
{
    private readonly IDistributedCache _distributedCache;
    protected readonly IDatabase Database;
    private readonly bool _isRedis = false;
    protected readonly string InstanceName;

    public DistributedCacheLockerBase(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
        if (distributedCache is RedisCache)
        {
            var redisConnectionWrapper = EngineContext.Current.Resolve<IRedisConnectionWrapper>();
            Database = redisConnectionWrapper.GetDatabaseAsync().Result;
            _isRedis = true;
            var cacheConfig = EngineContext.Current.GetAppModuleConfig<CacheConfig>();
            InstanceName = cacheConfig.DistributedCacheConfig.InstanceName;
        }
    }

    public async Task<bool> ExistAtomicLockerAsync(string key) =>
        _isRedis
            ? await Database.KeyExistsAsync($"{InstanceName}{key}")
            : !string.IsNullOrEmpty(await _distributedCache.GetStringAsync(key));

    public async Task<bool> SetAtomicLockerAsync(string key, TimeSpan expirationTime)
    {
        var isAcquired = false;

        if (_isRedis)
        {
            key = $"{InstanceName}{key}";
            //此处重写，防止在高并发下，多个进程同时获取到锁，导致同时执行任务
            isAcquired = await Database.StringSetAsync(
                key,
                key,
                expirationTime,
                When.NotExists // 确保键不存在时才设置值，防止多个进程同时设置
            );
        }
        else
        {
            await _distributedCache.SetStringAsync(
                key,
                key,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expirationTime
                }
            );
            isAcquired = true;
        }

        return isAcquired;
    }

    public Task RemoveAtomicLockerAsync(string key) =>
        _isRedis
            ? Database.KeyDeleteAsync($"{InstanceName}{key}")
            : _distributedCache.RemoveAsync(key);
}