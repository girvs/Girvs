namespace Girvs.Cache.Caching;

/// <summary>
/// 表示缓存对象的键
/// </summary>
public class CacheKey
{
    public CacheKey(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            throw new System.ArgumentException($"'{nameof(prefix)}' cannot be null or whitespace.", nameof(prefix));
        Prefix = prefix.Trim();
        var cacheConfig = EngineContext.Current.GetAppModuleConfig<CacheConfig>();
        CacheTime = cacheConfig.CacheBaseConfig.DefaultCacheTime;
        EnableCaching = cacheConfig.EnableCaching;
    }

    public CacheKey Create(string key = "", string otherKey = "", int? cacheTime = null)
    {
        Key = string.Format(Prefix, key);

        if (!string.IsNullOrEmpty(otherKey))
            Key += (":" + otherKey);

        CacheTime = cacheTime ?? CacheTime;
        return this;
    }

    /// <summary>
    /// Gets or sets a cache key
    /// </summary>
    public string Key { get; set; }

    public string Prefix { get; set; }

    /// <summary>
    /// Gets or sets a cache time in minutes
    /// </summary>
    public int CacheTime { get; set; }

    public bool EnableCaching { get; set; }
}