using Girvs.Cache.Caching;
using Microsoft.Extensions.Caching.Distributed;

namespace Girvs.Cache.CacheImps;

/// <summary>
/// Represents a redis distributed cache
/// </summary>
public partial class RedisCacheManager : DistributedCacheManager
{
    #region Fields

    private readonly IDistributedCache _distributedCache;
    private readonly IDatabase _database;
    protected readonly IRedisConnectionWrapper _connectionWrapper;

    #endregion

    #region Ctor

    public RedisCacheManager(
        AppSettings appSettings,
        IDistributedCache distributedCache,
        IRedisConnectionWrapper connectionWrapper,
        ICacheKeyManager cacheKeyManager,
        IConcurrentCollection<object> concurrentCollection
    )
        : base(appSettings, distributedCache, cacheKeyManager, concurrentCollection)
    {
        _distributedCache = distributedCache;
        _connectionWrapper = connectionWrapper;
        _database = _connectionWrapper.GetDatabase();
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Gets the list of cache keys prefix
    /// </summary>
    /// <param name="endPoint">Network address</param>
    /// <param name="prefix">String key pattern</param>
    /// <returns>List of cache keys</returns>
    protected virtual async Task<IEnumerable<RedisKey>> GetKeysAsync(
        EndPoint endPoint,
        string prefix = null
    )
    {
        return await (await _connectionWrapper.GetServerAsync(endPoint))
            .KeysAsync(
                (await _connectionWrapper.GetDatabaseAsync()).Database,
                string.IsNullOrEmpty(prefix) ? null : $"{prefix}*"
            )
            .ToListAsync();
    }

    #endregion

    #region Methods

    /// <summary>
    /// Remove items by cache key prefix
    /// </summary>
    /// <param name="prefix">Cache key prefix</param>
    /// <param name="prefixParameters">Parameters to create cache key prefix</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public override async Task RemoveByPrefixAsync(string prefix, params object[] prefixParameters)
    {
        prefix = PrepareKeyPrefix(prefix, prefixParameters);
        var db = await _connectionWrapper.GetDatabaseAsync();
        var instanceName = _appSettings.Get<CacheConfig>().DistributedCacheConfig.InstanceName ?? string.Empty;
        var fullPrefix = instanceName + prefix;

        foreach (var endPoint in await _connectionWrapper.GetEndPointsAsync())
        {
            // 获取当前节点所有匹配的键
            var keys = (await GetKeysAsync(endPoint, fullPrefix))
                .Select(k => (RedisKey) k);

            var isCluster = (await _connectionWrapper.GetServerAsync(endPoint)).ServerType == ServerType.Cluster;

            if (isCluster)
            {
                // 按哈希槽分组
                var slotGroups = keys.GroupBy(k => db.Multiplexer.GetHashSlot(k));

                foreach (var group in slotGroups)
                {
                    var keysInSlot = group.ToArray();
                    if (keysInSlot.Length > 0)
                    {
                        // 对同一槽的键执行批量删除
                        await db.KeyDeleteAsync(keysInSlot);
                    }
                }
            }
            else
            {
                db.KeyDelete(keys.ToArray());
            }
        }

        RemoveByPrefixInstanceData(prefix);
    }

    /// <summary>
    /// Clear all cache data
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    public override async Task ClearAsync()
    {
        await _connectionWrapper.FlushDatabaseAsync();

        ClearInstanceData();
    }

    public override async Task<bool> ExistAtomicLockerAsync(string key)
    {
        return await _database.KeyExistsAsync($"{_instanceName}{key}");
    }

    public override async Task<bool> SetAtomicLockerAsync(string key, TimeSpan expirationTime)
    {
        key = $"{_instanceName}{key}";
        //此处重写，防止在高并发下，多个进程同时获取到锁，导致同时执行任务
        return await _database.StringSetAsync(
            key,
            key,
            expirationTime,
            When.NotExists // 确保键不存在时才设置值，防止多个进程同时设置
        );
    }

    public override Task RemoveAtomicLockerAsync(string key)
    {
        return _database.KeyDeleteAsync($"{_instanceName}{key}");
    }

    #endregion
}