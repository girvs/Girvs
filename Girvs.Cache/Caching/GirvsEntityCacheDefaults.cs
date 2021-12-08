using System.Text;
using Girvs.BusinessBasis.Entities;
using Girvs.BusinessBasis.Repositories;
using Girvs.Infrastructure;

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
        private static string EntityTypeName => typeof(TEntity).Name.ToLowerInvariant();

        private static string TenantKey
        {
            get
            {
                var property = typeof(TEntity).GetProperty("TenantId");
                return property == null
                    ? string.Empty
                    : $":TenantId_{EngineContext.Current.ClaimManager.GetTenantId()}";
            }
        }

        private static string OtherQueryConditionKey
        {
            get
            {
                var repositoryOtherQueryCondition = EngineContext.Current.Resolve<IRepositoryOtherQueryCondition>();
                if (repositoryOtherQueryCondition == null)
                {
                    return string.Empty;
                }
                else
                {
                    var expression = repositoryOtherQueryCondition.GetOtherQueryCondition<TEntity>();
                    return
                        $":OtherQueryConditionKey_{HashHelper.CreateHash(Encoding.UTF8.GetBytes(expression.ToString()))}";
                }
            }
        }
        
        /// <summary>
        /// 通过租户标识符获取缓存实体的键
        /// </summary>
        /// <remarks>
        /// {0} : entity id
        /// </remarks>
        public static CacheKey ByTenantKey =>
            new CacheKey($"{EntityTypeName}{TenantKey}");

        /// <summary>
        /// 通过标识符获取缓存实体的键
        /// </summary>
        /// <remarks>
        /// {0} : entity id
        /// </remarks>
        public static CacheKey ByIdCacheKey =>
            new CacheKey($"{EntityTypeName}{TenantKey}:byid:{{0}}");

        /// <summary>
        /// 获取通过标识符缓存实体的键
        /// </summary>
        /// <remarks>
        /// {0} : entity ids
        /// </remarks>
        public static CacheKey ByIdsCacheKey =>
            new CacheKey($"{EntityTypeName}{TenantKey}:list{OtherQueryConditionKey}:byids:{{0}}");

        /// <summary>
        /// 获取缓存所有实体的键
        /// </summary>
        public static CacheKey AllCacheKey =>
            new CacheKey($"{EntityTypeName}{TenantKey}:list{OtherQueryConditionKey}:all:{{0}}");

        /// <summary>
        /// 获取所有列表页面的缓存
        /// </summary>
        public static CacheKey ListCacheKey =>
            new CacheKey($"{EntityTypeName}{TenantKey}:list{OtherQueryConditionKey}");

        /// <summary>
        /// 以租户为单位列表页面的缓存
        /// </summary>
        public static CacheKey TenantListCacheKey =>
            new CacheKey($"{EntityTypeName}{TenantKey}:list");

        /// <summary>
        /// 获取查询列表缓存键
        /// </summary>
        public static CacheKey QueryCacheKey =>
            new CacheKey($"{EntityTypeName}{TenantKey}:list{OtherQueryConditionKey}:query:{{0}}");

        /// <summary>
        /// 创建自定义的缓存键
        /// </summary>
        /// <param name="key">自定义缓存键</param>
        /// <returns></returns>
        public static CacheKey BuideCustomize(string key) => new CacheKey(key);
    }
}