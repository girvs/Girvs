using Girvs.BusinessBasis.Entities;

namespace Girvs.Cache.Caching
{
    /// <summary>
    /// 表示与缓存实体相关的默认值
    /// </summary>
    public static partial class GirvsEntityCacheDefaults<TEntity> where TEntity : Entity
    {
        /// <summary>
        /// 获取缓存键中使用的实体类型名称
        /// </summary>
        public static string EntityTypeName => typeof(TEntity).Name.ToLowerInvariant();

        /// <summary>
        /// 通过标识符获取缓存实体的键
        /// </summary>
        /// <remarks>
        /// {0} : entity id
        /// </remarks>
        public static CacheKey ByIdCacheKey => new CacheKey($"{EntityTypeName}:byid.{{0}}");

        /// <summary>
        /// 获取通过标识符缓存实体的键
        /// </summary>
        /// <remarks>
        /// {0} : entity ids
        /// </remarks>
        public static CacheKey ByIdsCacheKey => new CacheKey($"{ListCacheKey}:byids.{{0}}");

        /// <summary>
        /// 获取缓存所有实体的键
        /// </summary>
        public static CacheKey AllCacheKey => new CacheKey($"{ListCacheKey}:all");

        /// <summary>
        /// 获取所有列表页面的缓存
        /// </summary>
        public static CacheKey ListCacheKey => new CacheKey($"{EntityTypeName}:list");

        /// <summary>
        /// 获取查询列表缓存键
        /// </summary>
        public static CacheKey QueryCacheKey => new CacheKey($"{ListCacheKey}:query.{{0}}");

        /// <summary>
        /// 创建自定义的缓存键
        /// </summary>
        /// <param name="key">自定义缓存键</param>
        /// <returns></returns>
        public static CacheKey BuideCustomize(string key) => new CacheKey(key);
    }
}