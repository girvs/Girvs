namespace Girvs.Cache.Configuration
{
    public class RedisCacheConfig
    {
        public string ConnectionString { get; set; } = "127.0.0.1:6379,ssl=False";
    }
}