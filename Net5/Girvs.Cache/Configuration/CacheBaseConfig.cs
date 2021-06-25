namespace Girvs.Cache.Configuration
{
    /// <summary>
    /// 表示缓存配置参数
    /// </summary>
    public class CacheBaseConfig
    {
        /// <summary>
        /// 获取或设置以分钟为单位的默认缓存时间
        /// </summary>
        public int DefaultCacheTime { get; set; } = 60;

        /// <summary>
        /// 获取或设置以分钟为单位的短期缓存时间
        /// </summary>
        public int ShortTermCacheTime { get; set; } = 3;

        /// <summary>
        /// 获取或设置以分钟为单位的捆绑文件缓存时间
        /// </summary>
        public int BundledFilesCacheTime { get; set; } = 120;
    }
}