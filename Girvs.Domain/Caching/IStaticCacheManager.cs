using System;
using System.Threading.Tasks;

namespace Girvs.Domain.Caching
{
    /// <summary>
    /// 表示HTTP请求之间缓存的管理器（长期缓存）
    /// </summary>
    public interface IStaticCacheManager : ICacheManager
    {
        /// <summary>
        /// 获取缓存项目。如果它还没有在缓存中，则加载并缓存它
        /// </summary>
        /// <typeparam name="T">缓存项的类型</typeparam>
        /// <param name="key">Cache key</param>
        /// <param name="acquire">如果尚未在缓存中加载项目的功能</param>
        /// <param name="cacheTime">缓存时间（分钟）;传递0不缓存;传递null以使用默认时间</param>
        /// <returns>与指定键关联的缓存值</returns>
        Task<T> GetAsync<T>(string key, Func<Task<T>> acquire, int? cacheTime = null);
    }
}