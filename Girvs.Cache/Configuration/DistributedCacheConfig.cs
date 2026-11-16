namespace Girvs.Cache.Configuration;

/// <summary>
/// Represents distributed cache configuration parameters
/// </summary>
public partial class DistributedCacheConfig
{
    /// <summary>
    /// 缓存资源引用：索引 0 为 Resources 资源键格式串，索引 1 起为 string.Format 参数；
    /// 同一组参数既用于组合资源键，也用于格式化资源的 Settings:Endpoints。
    /// 例如 ["common-redis", "0"] 会引用资源 common-redis，并把 Endpoints 中的 {0} 替换为 0。
    /// </summary>
    public string[] ConnectionRef { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether we should use distributed cache
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Gets or sets schema name. Used when distributed cache is enabled and DistributedCacheType property is set as SqlServer
    /// </summary>
    public string SchemaName { get; set; } = "dbo";

    /// <summary>
    /// Gets or sets table name. Used when distributed cache is enabled and DistributedCacheType property is set as SqlServer
    /// </summary>
    public string TableName { get; set; } = "DistributedCache";

    /// <summary>
    /// Gets or sets instance name. Used when distributed cache is enabled and DistributedCacheType property is set as Redis or RedisSynchronizedMemory.
    /// Useful when one wants to partition a single Redis server for use with multiple apps, e.g. by setting InstanceName to "development" and "production".
    /// </summary>
    public string InstanceName { get; set; } = "girvs";

    /// <summary>
    /// Gets or sets the Redis event publish interval in milliseconds.
    /// Used when distributed cache is enabled and DistributedCacheType property is set as RedisSynchronizedMemory.
    /// If greater than zero, events will be buffered for this long before being published in batch, in order to reduce server load.
    /// If zero, events are published when they are raised, without buffering.
    /// </summary>
    public int PublishIntervalMs { get; set; } = 500;
}
