namespace Girvs.Cache.Caching;

/// <summary>
/// Represents key for caching objects
/// </summary>
public class CacheKey
{
    #region Ctor

    /// <summary>
    /// Initialize a new instance with key and prefixes
    /// </summary>
    /// <param name="key">Key</param>
    /// <param name="prefixes">Prefixes for remove by prefix functionality</param>
    public CacheKey(string key, params string[] prefixes)
    {
        Key = key;
        Prefixes.AddRange(prefixes.Where(prefix => !string.IsNullOrEmpty(prefix)));
    }

    public CacheKey(string key, int? cacheTime = null, params string[] prefixes)
        : this(key, prefixes)
    {
        if (cacheTime == null)
            CacheTime = Singleton<AppSettings>.Instance.Get<CacheConfig>().DefaultCacheTime;
        else
            this.CacheTime = cacheTime.Value;
    }

    #endregion

    #region Methods

    public CacheKey Create(string entityObjectKey = "", string otherKey = "", int? cacheTime = null)
    {
        var cacheKey = new CacheKey(Key, cacheTime, Prefixes.ToArray());
        if (entityObjectKey.IsNullOrEmpty())
            return cacheKey;

        cacheKey.Key = string.Format(Key, entityObjectKey);

        if (!string.IsNullOrEmpty(otherKey))
            cacheKey.Key += (":" + otherKey);

        return cacheKey;
    }

    /// <summary>
    /// Create a new instance from the current one and fill it with passed parameters
    /// </summary>
    /// <param name="createCacheKeyParameters">Function to create parameters</param>
    /// <param name="keyObjects">Objects to create parameters</param>
    /// <returns>Cache key</returns>
    public virtual CacheKey Create(
        Func<object, object> createCacheKeyParameters,
        params object[] keyObjects
    )
    {
        var cacheKey = new CacheKey(Key, Prefixes.ToArray());

        if (!keyObjects.Any())
            return cacheKey;

        cacheKey.Key = string.Format(
            cacheKey.Key,
            keyObjects.Select(createCacheKeyParameters).ToArray()
        );

        for (var i = 0; i < cacheKey.Prefixes.Count; i++)
            cacheKey.Prefixes[i] = string.Format(
                cacheKey.Prefixes[i],
                keyObjects.Select(createCacheKeyParameters).ToArray()
            );

        return cacheKey;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets a cache key
    /// </summary>
    public string Key { get; protected set; }

    /// <summary>
    /// Gets or sets prefixes for remove by prefix functionality
    /// </summary>
    public List<string> Prefixes { get; protected set; } = new();

    /// <summary>
    /// Gets or sets a cache time in minutes
    /// </summary>
    public int CacheTime { get; set; } =
        Singleton<AppSettings>.Instance.Get<CacheConfig>().DefaultCacheTime;

    #endregion
}
