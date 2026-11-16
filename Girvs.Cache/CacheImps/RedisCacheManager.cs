using Girvs.Cache.Caching;
using Microsoft.Extensions.Caching.Distributed;

namespace Girvs.Cache.CacheImps;

/// <summary>
/// Represents a redis distributed cache
/// </summary>
public partial class RedisCacheManager : DistributedCacheManager
{
    #region Fields

    // 基类已持有 IDistributedCache；此处不再重复声明，避免隐藏基类字段。
    // 不缓存 IDatabase：连接抖动时 RedisConnectionWrapper 会重建连接，
    // 旧 IDatabase 随旧连接一起失效，继续使用会抛 ObjectDisposedException。
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
        _connectionWrapper = connectionWrapper;
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
        var server = await _connectionWrapper.GetServerAsync(endPoint);
        var db = await _connectionWrapper.GetDatabaseAsync();

        var keys = server.KeysAsync(
            db.Database,
            string.IsNullOrEmpty(prefix) ? null : $"{prefix}*"
        );

        var result = new List<RedisKey>();
        await foreach (var key in keys)
        {
            result.Add(key);
        }

        return result;
    }

    /// <summary>
    /// 删除指定节点上匹配的键；集群模式按哈希槽分组批量删除
    /// </summary>
    /// <param name="endPoint">Network address</param>
    /// <param name="keys">待删除的键</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    protected virtual async Task DeleteKeysAsync(EndPoint endPoint, IEnumerable<RedisKey> keys)
    {
        var db = await _connectionWrapper.GetDatabaseAsync();
        var keyArray = keys as RedisKey[] ?? keys.ToArray();
        if (keyArray.Length == 0)
            return;

        var isCluster =
            (await _connectionWrapper.GetServerAsync(endPoint)).ServerType == ServerType.Cluster;

        if (isCluster)
        {
            // 按哈希槽分组，对同一槽的键执行批量删除
            foreach (var group in keyArray.GroupBy(k => db.Multiplexer.GetHashSlot(k)))
            {
                var keysInSlot = group.ToArray();
                if (keysInSlot.Length > 0)
                {
                    await db.KeyDeleteAsync(keysInSlot);
                }
            }
        }
        else
        {
            // 统一走异步 API，避免在异步链路中同步阻塞线程
            await db.KeyDeleteAsync(keyArray);
        }
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
        var instanceName = _instanceName ?? string.Empty;
        var fullPrefix = instanceName + prefix;

        foreach (var endPoint in await _connectionWrapper.GetEndPointsAsync())
        {
            // 副本上的键都会同步到主节点，跳过可避免重复扫描与删除
            if ((await _connectionWrapper.GetServerAsync(endPoint)).IsReplica)
                continue;

            // 获取当前节点所有匹配的键
            var keys = await GetKeysAsync(endPoint, fullPrefix);
            await DeleteKeysAsync(endPoint, keys);
        }

        RemoveByPrefixInstanceData(prefix);
    }

    /// <summary>
    /// Clear all cache data
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    public override async Task ClearAsync()
    {
        // 只清理当前实例命名空间下的键：原先的 FLUSHDB 会误删同一 Redis
        // 逻辑库中其他应用的数据。
        var instanceName = _instanceName ?? string.Empty;

        foreach (var endPoint in await _connectionWrapper.GetEndPointsAsync())
        {
            if ((await _connectionWrapper.GetServerAsync(endPoint)).IsReplica)
                continue;

            var keys = await GetKeysAsync(endPoint, instanceName);
            await DeleteKeysAsync(endPoint, keys);
        }

        ClearInstanceData();
    }

    public override async Task<bool> ExistAtomicLockerAsync(string key)
    {
        var database = await _connectionWrapper.GetDatabaseAsync();
        return await database.KeyExistsAsync($"{_instanceName}{key}");
    }

    public override async Task<bool> SetAtomicLockerAsync(string key, TimeSpan expirationTime)
    {
        key = $"{_instanceName}{key}";
        var database = await _connectionWrapper.GetDatabaseAsync();
        //此处重写，防止在高并发下，多个进程同时获取到锁，导致同时执行任务
        return await database.StringSetAsync(
            key,
            key,
            expirationTime,
            When.NotExists // 确保键不存在时才设置值，防止多个进程同时设置
        );
    }

    public override async Task RemoveAtomicLockerAsync(string key)
    {
        var database = await _connectionWrapper.GetDatabaseAsync();
        await database.KeyDeleteAsync($"{_instanceName}{key}");
    }

    #endregion
}
