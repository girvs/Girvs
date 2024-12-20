namespace Girvs.Cache.Caching;

/// <summary>
/// Represents a memory cache manager
/// </summary>
public partial class MemoryCacheManager : ILocker, IStaticCacheManager
{
    #region Fields

    // Flag: Has Dispose already been called?
    private bool _disposed;

    private readonly IMemoryCache _memoryCache;

    private static readonly ConcurrentDictionary<string, CancellationTokenSource> _prefixes =
        new ConcurrentDictionary<string, CancellationTokenSource>();

    private static CancellationTokenSource _clearToken = new CancellationTokenSource();

    #endregion

    #region Ctor

    public MemoryCacheManager(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Prepare cache entry options for the passed key
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <returns>Cache entry options</returns>
    private MemoryCacheEntryOptions PrepareEntryOptions(CacheKey key)
    {
        //set expiration time for the passed cache key
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(key.CacheTime)
        };

        //add tokens to clear cache entries
        options.AddExpirationToken(new CancellationChangeToken(_clearToken.Token));
        foreach (var keyPrefix in key.Prefix.Split(':').ToList())
        {
            var tokenSource = _prefixes.GetOrAdd(keyPrefix, new CancellationTokenSource());
            options.AddExpirationToken(new CancellationChangeToken(tokenSource.Token));
        }

        return options;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Get a cached item. If it's not in the cache yet, then load and cache it
    /// </summary>
    /// <typeparam name="T">Type of cached item</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="acquire">Function to load item if it's not in the cache yet</param>
    /// <returns>The cached value associated with the specified key</returns>
    public T Get<T>(CacheKey key, Func<T> acquire)
    {
        if (key.CacheTime <= 0 || !key.EnableCaching)
            return acquire();

        var result = _memoryCache.GetOrCreate(
            key.Key,
            entry =>
            {
                entry.SetOptions(PrepareEntryOptions(key));

                return acquire();
            }
        );

        //do not cache null value
        if (result == null)
            Remove(key);

        return result;
    }

    /// <summary>
    /// Removes the value with the specified key from the cache
    /// </summary>
    /// <param name="key">Key of cached item</param>
    public void Remove(CacheKey key)
    {
        _memoryCache.Remove(key.Key);
    }

    /// <summary>
    /// Get a cached item. If it's not in the cache yet, then load and cache it
    /// </summary>
    /// <typeparam name="T">Type of cached item</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="acquire">Function to load item if it's not in the cache yet</param>
    /// <returns>The cached value associated with the specified key</returns>
    public async Task<T> GetAsync<T>(CacheKey key, Func<Task<T>> acquire)
    {
        if (key.CacheTime <= 0 || !key.EnableCaching)
            return await acquire();

        var result = await _memoryCache.GetOrCreateAsync(
            key.Key,
            async entry =>
            {
                entry.SetOptions(PrepareEntryOptions(key));

                return await acquire();
            }
        );

        //do not cache null value
        if (result == null)
            Remove(key);

        return result;
    }

    /// <summary>
    /// Adds the specified key and object to the cache
    /// </summary>
    /// <param name="key">Key of cached item</param>
    /// <param name="data">Value for caching</param>
    public void Set(CacheKey key, object data)
    {
        if (key.CacheTime <= 0 || data == null || !key.EnableCaching)
            return;

        _memoryCache.Set(key.Key, data, PrepareEntryOptions(key));
    }

    /// <summary>
    /// Gets a value indicating whether the value associated with the specified key is cached
    /// </summary>
    /// <param name="key">Key of cached item</param>
    /// <returns>True if item already is in cache; otherwise false</returns>
    public bool IsSet(CacheKey key)
    {
        return _memoryCache.TryGetValue(key.Key, out _);
    }

    /// <summary>
    /// Perform some action with exclusive in-memory lock
    /// </summary>
    /// <param name="key">The key we are locking on</param>
    /// <param name="expirationTime">The time after which the lock will automatically be expired</param>
    /// <param name="action">Action to be performed with locking</param>
    /// <param name="immediateLockDispose"></param>
    /// <returns>True if lock was acquired and action was performed; otherwise false</returns>
    public async Task<bool> PerformActionWithLock(
        string key,
        TimeSpan expirationTime,
        Func<Task> action,
        bool immediateLockDispose = true
    )
    {
        //ensure that lock is acquired
        if (IsSet(new CacheKey(key)))
            return false;

        try
        {
            _memoryCache.Set(key, key, expirationTime);

            //perform action
            await action();

            return true;
        }
        finally
        {
            if (immediateLockDispose)
                //release lock even if action fails
                Remove(key);
        }
    }

    /// <summary>
    /// Removes the value with the specified key from the cache
    /// </summary>
    /// <param name="key">Key of cached item</param>
    public void Remove(string key)
    {
        _memoryCache.Remove(key);
    }

    /// <summary>
    /// Removes items by key prefix
    /// </summary>
    /// <param name="prefix">String key prefix</param>
    public void RemoveByPrefix(string prefix)
    {
        _prefixes.TryRemove(prefix, out var tokenSource);
        tokenSource?.Cancel();
        tokenSource?.Dispose();
    }

    /// <summary>
    /// Clear all cache data
    /// </summary>
    public void Clear()
    {
        _clearToken.Cancel();
        _clearToken.Dispose();

        _clearToken = new CancellationTokenSource();

        foreach (var prefix in _prefixes.Keys.ToList())
        {
            _prefixes.TryRemove(prefix, out var tokenSource);
            tokenSource?.Dispose();
        }
    }

    private static Dictionary<string, long> IncrementNumber = new Dictionary<string, long>();

    private static object asyncObj = new object();

    public long StringIncrement(string key)
    {
        lock (asyncObj)
        {
            if (IncrementNumber.ContainsKey(key))
            {
                IncrementNumber[key]++;
                return IncrementNumber[key];
            }
            else
            {
                IncrementNumber.Add(key, 1);
                return 1;
            }
        }
    }

    public List<string> GetCacheKeys()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        //var entries = provider.GetType().GetField("_entries", flags)?.GetValue(provider);
        var keys = new List<string>();
        var cache = _memoryCache.GetType().GetField("_cache", flags)?.GetValue(_memoryCache);
        if (cache != null)
        {
            if (cache.GetType().GetField("_memory", flags)?.GetValue(cache) is IDictionary memory)
            {
                foreach (DictionaryEntry entry in memory)
                {
                    keys.Add(entry.Key.ToString());
                }
            }
        }

        //var cacheItems = entries as IDictionary;
        //if (cacheItems == null) return keys;

        return keys;
    }

    /// <summary>
    /// Dispose cache manager
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    // Protected implementation of Dispose pattern.
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _memoryCache.Dispose();
        }

        _disposed = true;
    }

    #endregion
}
