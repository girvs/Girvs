using System.Runtime.Serialization;

namespace Girvs.Cache.Configuration
{
    /// <summary>
    /// 表示分布式缓存类型枚举
    /// </summary>
    public enum CacheType
    {
        [EnumMember(Value = "memory")] Memory,
        [EnumMember(Value = "redis")] Redis,
        [EnumMember(Value = "dapr")] Dapr
    }
}