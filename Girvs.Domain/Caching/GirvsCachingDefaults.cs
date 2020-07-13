using System;

namespace Girvs.Domain.Caching
{
    /// <summary>
    /// 表示与缓存相关的默认值
    /// </summary>
    public static partial class GirvsCachingDefaults
    {
        public static bool DefaultUseCache => true;
        /// <summary>
        /// 以分钟为单位获取默认缓存时间
        /// </summary>
        public const int CacheTime = 60;

        /// <summary>
        /// 获取用于将保护锁列表存储到Redis的密钥（与启用的PersistDataProtectionKeysToRedis选项一起使用）
        /// </summary>
        public static string RedisDataProtectionKey => "SmartProducts.DataProtectionKeys";

        public static string RedisDefaultPrefix => AppDomain.CurrentDomain.FriendlyName.Replace(".", "_");
    }
}