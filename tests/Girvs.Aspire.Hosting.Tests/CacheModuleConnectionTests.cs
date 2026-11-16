using System.Reflection;
using Girvs.Cache;
using Girvs.Cache.CacheImps;
using Girvs.Cache.Caching;
using Girvs.Cache.Configuration;
using Girvs.Configuration;
using Girvs.Infrastructure;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Girvs.Aspire.Hosting.Tests;

[Collection(CacheModuleCollection.Name)]
public class CacheModuleConnectionTests
{
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(2);

    [Theory]
    [InlineData("redis")]
    [InlineData("redis-synchronized-memory")]
    public async Task GetDatabase_连接配置无效时同步和异步均返回配置异常(string resourceType)
    {
        using var fixture = new CacheModuleFixture(resourceType);
        var wrapper = fixture.Services.GetRequiredService<IRedisConnectionWrapper>();

        // 先走异步入口：共享工厂若再次自锁，将以超时失败，避免阻塞同步测试线程。
        await Assert.ThrowsAsync<ArgumentException>(() =>
            wrapper.GetDatabaseAsync().WaitAsync(OperationTimeout)
        );
        Assert.Throws<ArgumentException>(() => wrapper.GetDatabase());

        if (resourceType == "redis-synchronized-memory")
            Assert.Throws<ArgumentException>(() =>
                fixture.Services.GetRequiredService<IStaticCacheManager>()
            );
    }

    [Theory]
    [InlineData("redis")]
    [InlineData("redis-synchronized-memory")]
    public async Task GetAsync_分布式缓存连接配置无效时返回配置异常(string resourceType)
    {
        using var fixture = new CacheModuleFixture(resourceType);
        var cache = fixture.Services.GetRequiredService<IDistributedCache>();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            cache.GetAsync("test-key").WaitAsync(OperationTimeout)
        );
    }

    [Theory]
    [InlineData("redis")]
    [InlineData("redis-synchronized-memory")]
    public async Task GetDatabase_宿主自定义工厂可用于同步和异步获取数据库(string resourceType)
    {
        var database = DispatchProxy.Create<IDatabase, RedisStub>();
        var connection = DispatchProxy.Create<IConnectionMultiplexer, RedisStub>();
        var connectionStub = (RedisStub)connection;
        connectionStub.Database = database;
        connectionStub.Subscriber = DispatchProxy.Create<ISubscriber, RedisStub>();
        using var fixture = new CacheModuleFixture(
            resourceType,
            services => services.Configure<RedisCacheOptions>(options =>
                options.ConnectionMultiplexerFactory = () => Task.FromResult(connection)
            )
        );
        var wrapper = fixture.Services.GetRequiredService<IRedisConnectionWrapper>();

        Assert.Same(database, await wrapper.GetDatabaseAsync().WaitAsync(OperationTimeout));
        Assert.Same(database, wrapper.GetDatabase());

        if (resourceType == "redis-synchronized-memory")
        {
            var manager = fixture.Services.GetRequiredService<IStaticCacheManager>();
            var key = new CacheKey("test-key") { CacheTime = 1 };
            await manager.SetAsync(key, "test-value");
            Assert.Equal("test-value", await manager.GetAsync<string>(key));
        }
    }

    private sealed class CacheModuleFixture : IDisposable
    {
        private readonly AppSettings _previous = Singleton<AppSettings>.Instance;
        private readonly IMemoryCache _memoryCache;

        public CacheModuleFixture(
            string resourceType,
            Action<IServiceCollection> configureServices = null
        )
        {
            var settings = new AppSettings();
            settings.PreLoadModelConfig();
            settings["CacheConfig"] = new CacheConfig
            {
                DistributedCacheConfig = new DistributedCacheConfig
                {
                    Enabled = true,
                    ConnectionRef = ["test-redis"],
                    PublishIntervalMs = 0,
                },
            };
            settings.Resources["test-redis"] = new GirvsInfrastructureResource
            {
                Type = resourceType,
                // 未知选项会在解析阶段报错，不会尝试连接网络。
                Settings = new Dictionary<string, string>
                {
                    ["Endpoints"] = "invalid-test-option=true",
                },
            };
            Singleton<AppSettings>.Instance = settings;

            var services = new ServiceCollection();
            services.AddSingleton(settings);
            configureServices?.Invoke(services);
            new GirvsCacheModule().ConfigureServices(services, null);
            Services = services.BuildServiceProvider();
            _memoryCache = Services.GetService<IMemoryCache>();
        }

        public ServiceProvider Services { get; }

        public void Dispose()
        {
            try
            {
                Services.Dispose();
            }
            finally
            {
                // 模块当前采用实例注册，测试自行释放缓存，避免遗留测试资源。
                _memoryCache?.Dispose();
                Singleton<AppSettings>.Instance = _previous;
            }
        }
    }

    // 仅替代外部 Redis SDK 接口，模块注册、连接包装器和缓存管理器均使用真实实现。
    public class RedisStub : DispatchProxy
    {
        public IDatabase Database { get; set; }
        public ISubscriber Subscriber { get; set; }

        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            return targetMethod.Name switch
            {
                "get_IsConnected" => true,
                "get_Database" => 0,
                "GetDatabase" => Database,
                "GetSubscriber" => Subscriber,
                "Subscribe" or "Unsubscribe" or "Dispose" => null,
                _ => throw new NotSupportedException(targetMethod.Name),
            };
        }
    }
}
