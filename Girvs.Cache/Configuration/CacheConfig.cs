namespace Girvs.Cache.Configuration;

/// <summary>
/// 代表分布式缓存配置参数
/// </summary>
public class CacheConfig : IAppModuleConfig
{
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Gets or sets the default cache time in minutes
    /// </summary>
    public int DefaultCacheTime { get; protected set; } = 60;

    /// <summary>
    /// 分布式缓存配置
    /// </summary>
    public DistributedCacheConfig DistributedCacheConfig { get; set; }

    // /// <summary>
    // /// Gets or sets whether to disable linq2db query cache
    // /// </summary>
    // public bool LinqDisableQueryCache { get; protected set; } = false;
    //
    // /// <summary>
    // /// 获取或设置分布式缓存类型
    // /// </summary>
    // [JsonConverter(typeof(JsonStringEnumConverter))]
    // public CacheType DistributedCacheType { get; set; } = CacheType.Redis;
    // public CacheBaseConfig CacheBaseConfig { get; set; } = new CacheBaseConfig();
    // public MemoryCacheConfig MemoryCacheConfig { get; set; } = new MemoryCacheConfig();
    // public RedisCacheConfig RedisCacheConfig { get; set; } = new RedisCacheConfig();

    public void Init() { }
}
