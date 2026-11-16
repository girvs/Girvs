using Girvs;
using Girvs.Cache;
using Girvs.Cache.CacheImps;
using Girvs.Cache.Caching;
using Girvs.Cache.Configuration;
using Girvs.Configuration;
using Girvs.Configuration.Resources;
using Girvs.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

// 独立诊断程序：不连接真实 Redis，不修改生产代码。
var previous = Singleton<AppSettings>.Instance;
var failures = 0;
const string ProbeConnectionString = "probe-invalid-option=true";
Console.WriteLine($"运行环境: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
try
{
    var invalidProbeRejected = false;
    try { ConfigurationOptions.Parse(ProbeConnectionString); }
    catch (ArgumentException) { invalidProbeRejected = true; }
    Check(invalidProbeRejected, "探针连接串在解析阶段失败，避免连接真实 Redis", "未知配置项被拒绝");
    var services = CreateServices("redis");
    services.AddSingleton<RedisConnectionWrapper, TracedWrapper>();
    using var provider = services.BuildServiceProvider();
    var wrapper = (TracedWrapper)provider.GetRequiredService<RedisConnectionWrapper>();
    Check(provider.GetRequiredService<IOptions<RedisCacheOptions>>().Value != null,
        "Options 构建及包装器解析可以完成", "已同步完成服务解析");
    var connection = wrapper.GetConnectionMultiplexerAsync();
    var completed = await Task.WhenAny(connection, Task.Delay(500)) == connection;
    Check(completed, "Redis 首次异步连接必须能完成或报错",
        $"IsCompleted={connection.IsCompleted}, GetConnectionAsync={wrapper.AsyncCalls}, ConnectAsync={wrapper.ConnectCalls}, SemaphoreCount={wrapper.LockCount}");

    // 用立即抛错的工厂替换共享回调，验证包装器不再自锁；无网络访问。
    var controlServices = CreateServices("redis");
    controlServices.Configure<RedisCacheOptions>(o =>
        o.ConnectionMultiplexerFactory = () => Task.FromException<IConnectionMultiplexer>(new ProbeException()));
    using var control = controlServices.BuildServiceProvider();
    try
    {
        await control.GetRequiredService<RedisConnectionWrapper>().GetConnectionMultiplexerAsync();
        Check(false, "替换工厂后不再自锁", "未触发预期的探针异常");
    }
    catch (ProbeException)
    {
        Check(true, "替换工厂后不再自锁", "立即收到 ProbeException");
    }

    using var cacheProvider = CreateServices("redis").BuildServiceProvider();
    var read = cacheProvider.GetRequiredService<IDistributedCache>().GetAsync("probe-key");
    Check(await Task.WhenAny(read, Task.Delay(500)) == read,
        "IDistributedCache 首次读取必须能完成或报错", $"IsCompleted={read.IsCompleted}");

    // 同步构造会阻塞后台线程；保留此容器至诊断进程退出，避免释放与构造竞争。
    var syncServices = CreateServices("redis-synchronized-memory");
    syncServices.AddSingleton<RedisConnectionWrapper, TracedWrapper>();
    var syncProvider = syncServices.BuildServiceProvider();
    var syncWrapper = (TracedWrapper)syncProvider.GetRequiredService<RedisConnectionWrapper>();
    var resolve = Task.Run(() => syncProvider.GetRequiredService<IStaticCacheManager>());
    Check(await Task.WhenAny(resolve, Task.Delay(500)) == resolve,
        "同步内存模式首次解析必须能完成或报错",
        $"IsCompleted={resolve.IsCompleted}, GetConnectionAsync={syncWrapper.AsyncCalls}, SemaphoreCount={syncWrapper.LockCount}");

    foreach (var type in new[] { "sqlserver", "redsi", "" })
    {
        var rejected = false;
        ServiceCollection invalid = null;
        try { invalid = CreateServices(type); }
        catch (GirvsException) { rejected = true; }
        Check(rejected, $"不支持的资源类型 '{type}' 应在注册时明确报错",
            $"注册抛错={rejected}, IStaticCacheManager={invalid?.Any(d => d.ServiceType == typeof(IStaticCacheManager))}");
        if (invalid != null)
        {
            using var invalidProvider = invalid.BuildServiceProvider();
            try { invalidProvider.GetRequiredService<ILocker>(); }
            catch (Exception ex) { Console.WriteLine($"  实际解析错误: {ex.Message}"); }
        }
    }

    var memoryServices = CreateServices("disabled");
    var memoryProvider = memoryServices.BuildServiceProvider();
    var memory = memoryProvider.GetRequiredService<IMemoryCache>();
    memoryProvider.Dispose();
    var disposed = false;
    try { memory.Set("after-provider-dispose", 1); }
    catch (ObjectDisposedException) { disposed = true; }
    Check(disposed, "容器释放后应释放模块自行创建的 MemoryCache", $"MemoryCache 已释放={disposed}");
    memory.Dispose();

    var configuredServices = CreateServices("disabled", configureMemory: true);
    using var configuredProvider = configuredServices.BuildServiceProvider();
    var limitEnforced = false;
    try { configuredProvider.GetRequiredService<IMemoryCache>().Set("no-size", 1); }
    catch (InvalidOperationException) { limitEnforced = true; }
    Check(limitEnforced, "保留宿主配置的 MemoryCache SizeLimit", $"SizeLimit 生效={limitEnforced}");

    var standardServices = new ServiceCollection();
    standardServices.AddMemoryCache(o => o.SizeLimit = 1);
    var standardProvider = standardServices.BuildServiceProvider();
    var standardMemory = standardProvider.GetRequiredService<IMemoryCache>();
    var standardLimitEnforced = false;
    try { standardMemory.Set("no-size-control", 1); }
    catch (InvalidOperationException) { standardLimitEnforced = true; }
    standardProvider.Dispose();
    var standardDisposed = false;
    try { standardMemory.Set("disposed-control", 1); }
    catch (ObjectDisposedException) { standardDisposed = true; }
    Check(standardLimitEnforced && standardDisposed, "AddMemoryCache 对照组遵守配置并由容器释放",
        $"SizeLimit 生效={standardLimitEnforced}, 已释放={standardDisposed}");

    using var memoryControl = CreateServices("disabled").BuildServiceProvider();
    var manager = memoryControl.GetRequiredService<IStaticCacheManager>();
    var key = new CacheKey("memory-control") { CacheTime = 1 };
    await manager.SetAsync(key, "value");
    Check(await manager.GetAsync<string>(key) == "value", "默认内存缓存读写正常", "写入后命中");

    var emptyRefServices = CreateServices("redis", emptyReference: true);
    using var emptyRefProvider = emptyRefServices.BuildServiceProvider();
    Check(emptyRefProvider.GetRequiredService<IStaticCacheManager>() is MemoryCacheManager,
        "启用分布式缓存但未配置引用时维持现有内存回退行为", "解析为 MemoryCacheManager");

    using var caseProvider = CreateServices("ReDiS").BuildServiceProvider();
    Check(caseProvider.GetRequiredService<IOptions<RedisCacheOptions>>().Value.Configuration == ProbeConnectionString,
        "Redis 类型匹配不区分大小写", "资源端点成功传入 Options");

    var invalidEndpointsRejected = false;
    try { CreateServices("redis", missingEndpoints: true); }
    catch (GirvsException) { invalidEndpointsRejected = true; }
    Check(invalidEndpointsRejected, "缺少 Redis Endpoints 时立即报错", "捕获 GirvsException");
}
finally
{
    Singleton<AppSettings>.Instance = previous;
}
Console.WriteLine($"诊断失败断言数: {failures}");
return failures == 0 ? 0 : 1;

void Check(bool success, string name, string detail)
{
    Console.WriteLine($"{(success ? "PASS" : "FAIL")} {name}: {detail}");
    if (!success) failures++;
}

ServiceCollection CreateServices(string resourceType, bool configureMemory = false,
    bool emptyReference = false, bool missingEndpoints = false)
{
    var settings = new AppSettings();
    settings.PreLoadModelConfig();
    settings["CacheConfig"] = new CacheConfig
    {
        DistributedCacheConfig = new DistributedCacheConfig
        {
            Enabled = resourceType != "disabled",
            ConnectionRef = emptyReference ? [] : ["probe-redis"],
            PublishIntervalMs = 0
        }
    };
    settings.Resources["probe-redis"] = new GirvsInfrastructureResource
    {
        Type = resourceType,
        Settings = missingEndpoints ? [] : new Dictionary<string, string> { ["Endpoints"] = ProbeConnectionString }
    };
    Singleton<AppSettings>.Instance = settings;
    var services = new ServiceCollection();
    services.AddSingleton(settings);
    if (configureMemory) services.AddMemoryCache(o => o.SizeLimit = 1);
    new GirvsCacheModule().ConfigureServices(services, null);
    return services;
}

sealed class ProbeException : Exception;

sealed class TracedWrapper(IOptions<RedisCacheOptions> options) : RedisConnectionWrapper(options)
{
    public int AsyncCalls;
    public int ConnectCalls;
    public int LockCount => _connectionLock.CurrentCount;

    protected override Task<IConnectionMultiplexer> GetConnectionAsync()
    {
        Interlocked.Increment(ref AsyncCalls);
        return base.GetConnectionAsync();
    }

    protected override Task<IConnectionMultiplexer> ConnectAsync()
    {
        Interlocked.Increment(ref ConnectCalls);
        return base.ConnectAsync();
    }
}
