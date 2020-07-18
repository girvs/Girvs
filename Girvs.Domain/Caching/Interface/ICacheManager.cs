using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Girvs.Domain.Caching.Interface
{
    /// <summary>
    /// 缓存管理器界面
    /// </summary>
    public interface ICacheManager : IDisposable
    {
        /// <summary>
        /// 获取缓存项目。如果它还没有在缓存中，则加载并缓存它
        /// </summary>
        /// <typeparam name="T">缓存项的类型</typeparam>
        /// <param name="key">缓存密钥</param>
        /// <param name="acquire">如果尚未在缓存中加载项目的功能</param>
        /// <param name="cacheTime">缓存时间（分钟）;传递0不缓存;传递null以使用默认时间</param>
        /// <returns>与指定键关联的缓存值</returns>
        Task<T> Get<T>(string key, Func<T> acquire, int? cacheTime = null);

        Task<string> GetToString(string key);

        /// <summary>
        /// 将指定的键和对象添加到缓存中
        /// </summary>
        /// <param name="key">缓存项目的关键</param>
        /// <param name="data">缓存的价值</param>
        /// <param name="cacheTime">缓存时间以分钟为单位</param>
        Task Set(string key, object data, int? cacheTime);

        /// <summary>
        /// 获取一个值，该值指示是否缓存与指定键关联的值
        /// </summary>
        /// <param name="key">缓存项目的关键</param>
        /// <returns>如果项目已在缓存中，则为True;否则是假的</returns>
        Task<bool> IsSet(string key);

        /// <summary>
        /// 从缓存中删除具有指定键的值
        /// </summary>
        /// <param name="key">缓存项目的关键</param>
        Task Remove(string key);

        /// <summary>
        /// 按键前缀删除项目
        /// </summary>
        /// <param name="prefix">字符串键前缀</param>
        Task RemoveByPrefix(string prefix);

        /// <summary>
        /// 清除所有缓存数据
        /// </summary>
        Task Clear();

        /// <summary>
        /// 获取缓存密钥
        /// </summary>
        /// <returns>keys</returns>
        Task<List<string>> GetCacheKeys();
    }
}
