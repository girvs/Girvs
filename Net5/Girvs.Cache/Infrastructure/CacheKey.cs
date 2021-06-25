using Girvs.Cache.Configuration;
using Girvs.Configuration;
using Girvs.Infrastructure;

namespace Girvs.Cache
{
    /// <summary>
    /// 表示缓存对象的键
    /// </summary>
    public class CacheKey
    {
        private readonly CacheConfig _cacheConfig;
        public CacheKey(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                throw new System.ArgumentException($"'{nameof(prefix)}' cannot be null or whitespace.", nameof(prefix));
            Prefix = prefix.Trim();
            var appSettings = Singleton<AppSettings>.Instance;
            _cacheConfig = appSettings.ModuleConfigurations[nameof(CacheConfig)] as CacheConfig;
            CacheTime = _cacheConfig.CacheBaseConfig.DefaultCacheTime;
        }

        public CacheKey Create(string key)
        {
            Key = string.Format(Prefix, key);
            return this;
        }

        /// <summary>
        /// Gets or sets a cache key
        /// </summary>
        public string Key { get; protected set; }

        public string Prefix { get; protected set; }

        /// <summary>
        /// Gets or sets a cache time in minutes
        /// </summary>
        public int CacheTime { get; private set; }
    }
}