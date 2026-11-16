using Girvs.Cache;
using Girvs.Cache.CacheImps;
using Girvs.Cache.Caching;
using Girvs.Cache.Configuration;
using Girvs.Configuration;
using Girvs.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.Aspire.Hosting.Tests;

[CollectionDefinition(Name)]
public sealed class CacheModuleCollection
{
    public const string Name = "CacheModule";
}

/// <summary>
/// 验证 GirvsCacheModule 消费字符串数组形式的 ConnectionRef：
/// 索引 0 为资源键格式串，索引 1 起为格式化参数。
/// </summary>
[Collection(CacheModuleCollection.Name)]
public class CacheModuleConnectionRefTests
{
    [Fact]
    public void 启用缓存且引用共享模板资源时注册Redis缓存实现()
    {
        var previous = Singleton<AppSettings>.Instance;
        try
        {
            var cacheConfig = new CacheConfig
            {
                DistributedCacheConfig = new DistributedCacheConfig
                {
                    Enabled = true,
                    ConnectionRef = ["common-redis", "2"],
                },
            };

            var appSettings = new AppSettings();
            appSettings.PreLoadModelConfig();
            appSettings["CacheConfig"] = cacheConfig;
            appSettings.Resources["common-redis"] = new GirvsInfrastructureResource
            {
                Type = "redis",
                Settings = new Dictionary<string, string>
                {
                    ["Endpoints"] = "192.168.51.183:6379,ssl=False,defaultdatabase={0}",
                },
            };
            Singleton<AppSettings>.Instance = appSettings;

            var services = new ServiceCollection();
            new GirvsCacheModule().ConfigureServices(services, null);

            Assert.Contains(
                services,
                descriptor =>
                    descriptor.ServiceType == typeof(IStaticCacheManager)
                    && descriptor.ImplementationType == typeof(RedisCacheManager)
            );
        }
        finally
        {
            Singleton<AppSettings>.Instance = previous;
        }
    }

    [Fact]
    public void 缓存未配置有效引用时回退内存缓存实现()
    {
        var previous = Singleton<AppSettings>.Instance;
        try
        {
            var cacheConfig = new CacheConfig
            {
                DistributedCacheConfig = new DistributedCacheConfig
                {
                    Enabled = true,
                    ConnectionRef = [],
                },
            };

            var appSettings = new AppSettings();
            appSettings.PreLoadModelConfig();
            appSettings["CacheConfig"] = cacheConfig;
            Singleton<AppSettings>.Instance = appSettings;

            var services = new ServiceCollection();
            new GirvsCacheModule().ConfigureServices(services, null);

            Assert.Contains(
                services,
                descriptor =>
                    descriptor.ServiceType == typeof(IStaticCacheManager)
                    && descriptor.ImplementationType == typeof(MemoryCacheManager)
            );
        }
        finally
        {
            Singleton<AppSettings>.Instance = previous;
        }
    }
}
