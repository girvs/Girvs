namespace Girvs.Configuration
{
    public class CacheConfig : IConfig
    {
        /// <summary>
        /// 获取或设置默认的缓存时间（以分钟为单位）
        /// </summary>
        public int DefaultCacheTime { get; set; } = 60;

        /// <summary>
        /// 获取或设置以分钟为单位的短期缓存时间
        /// </summary>
        public int ShortTermCacheTime { get; set; } = 3;

        /// <summary>
        /// 获取或设置捆绑文件的缓存时间（以分钟为单位）
        /// </summary>
        public int BundledFilesCacheTime { get; set; } = 120;
    }
}