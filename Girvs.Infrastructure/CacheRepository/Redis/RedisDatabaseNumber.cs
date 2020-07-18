namespace Girvs.Infrastructure.CacheRepository.Redis
{
    /// <summary>
    /// 表示redis数据库编号枚举
    /// </summary>
    public enum RedisDatabaseNumber
    {
        /// <summary>
        /// 用于缓存的数据库
        /// </summary>
        Cache = 1,
        /// <summary>
        /// 插件数据库
        /// </summary>
        Plugin = 2,
        /// <summary>
        /// 数据保护密钥数据库
        /// </summary>
        DataProtectionKeys = 3
    }
}
