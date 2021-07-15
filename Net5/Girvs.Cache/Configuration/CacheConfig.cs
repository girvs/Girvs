using System.Text.Json.Serialization;
using Girvs.Configuration;

namespace Girvs.Cache.Configuration
{
    /// <summary>
    /// 代表分布式缓存配置参数
    /// </summary>
    public class CacheConfig : IAppModuleConfig
    {
        public bool EnableCaching { get; set; } = true;
        /// <summary>
        /// 获取或设置分布式缓存类型
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public CacheType DistributedCacheType { get; set; } = CacheType.Redis;
        public CacheBaseConfig CacheBaseConfig { get; set; } = new CacheBaseConfig();
        public MemoryCacheConfig MemoryCacheConfig { get; set; } = new MemoryCacheConfig();
        public RedisCacheConfig RedisCacheConfig { get; set; } = new RedisCacheConfig();
        public void Init()
        {
            
        }
    }
}