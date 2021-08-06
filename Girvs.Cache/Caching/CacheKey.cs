// using System;
// using System.Collections.Generic;
// using System.Linq;
//
// namespace Girvs.Cache.Caching
// {
//     public partial class CacheKey
//     {
//         #region Fields
//
//         protected string _keyFormat = "";
//
//         #endregion
//
//         #region Ctor
//
//         public CacheKey(CacheKey cacheKey, Func<object, object> createCacheKeyParameters, params object[] keyObjects)
//         {
//             Init(cacheKey.Key, cacheKey.CacheTime, cacheKey.Prefixes.ToArray());
//
//             if(!keyObjects.Any())
//                 return;
//
//             Key = string.Format(_keyFormat, keyObjects.Select(createCacheKeyParameters).ToArray());
//
//             for (var i = 0; i < Prefixes.Count; i++)
//                 Prefixes[i] = string.Format(Prefixes[i], keyObjects.Select(createCacheKeyParameters).ToArray());
//         }
//
//         public CacheKey(string cacheKey, int? cacheTime = null, params string[] prefixes)
//         {
//             Init(cacheKey, cacheTime, prefixes);
//         }
//
//         public CacheKey(string cacheKey, params string[] prefixes)
//         {
//             Init(cacheKey, null, prefixes);
//         }
//
//         #endregion
//
//         #region Utilities
//
//         /// <summary>
//         /// Init instance of CacheKey
//         /// </summary>
//         /// <param name="cacheKey">Cache key</param>
//         /// <param name="cacheTime">Cache time; set to null to use the default value</param>
//         /// <param name="prefixes">Prefixes to remove by prefix functionality</param>
//         protected void Init(string cacheKey, int? cacheTime = null, params string[] prefixes)
//         {
//             Key = cacheKey;
//
//             _keyFormat = cacheKey;
//
//             if (cacheTime.HasValue)
//                 CacheTime = cacheTime.Value;
//
//             Prefixes.AddRange(prefixes.Where(prefix=> !string.IsNullOrEmpty(prefix)));
//         }
//
//         #endregion
//
//         /// <summary>
//         /// Cache key
//         /// </summary>
//         public string Key { get; protected set; }
//
//         /// <summary>
//         /// Prefixes to remove by prefix functionality
//         /// </summary>
//         public List<string> Prefixes { get; protected set; } = new List<string>();
//
//         /// <summary>
//         /// Cache time in minutes
//         /// </summary>
//         public int CacheTime { get; set; } = NopCachingDefaults.CacheTime;
//     }
// }

using Girvs.Cache.Configuration;
using Girvs.Configuration;
using Girvs.Extensions;
using Girvs.Infrastructure;

namespace Girvs.Cache.Caching
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
            _cacheConfig = EngineContext.Current.GetAppModuleConfig<CacheConfig>();
            CacheTime = _cacheConfig.CacheBaseConfig.DefaultCacheTime;
            EnableCaching = _cacheConfig.EnableCaching;
        }

        public CacheKey Create(string key = "", string otherKey = "", int? cacheTime = null)
        {
            Key = string.Format(Prefix, key);

            if (otherKey.IsNullOrWhiteSpace())
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

        public bool EnableCaching { get; set; } = true;
    }
}