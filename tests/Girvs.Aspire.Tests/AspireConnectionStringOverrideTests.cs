using Girvs.Cache.Configuration;
using Girvs.EntityFrameworkCore.Configuration;
using Girvs.EventBus.Configuration;

namespace Girvs.Aspire.Tests;

/// <summary>
/// 覆盖各组件自己实现的 Aspire 连接串覆盖方法（CacheConfig/EventBusConfig/DbConfig 等）。
/// 映射逻辑本身归属各组件模块，此处仅验证 Aspire 场景下的端到端行为。
/// </summary>
public class AspireConnectionStringOverrideTests
{
    private static IConfiguration BuildConfiguration(
        params (string Name, string Value)[] connectionStrings
    )
    {
        var data = connectionStrings.ToDictionary(
            x => $"ConnectionStrings:{x.Name}",
            x => x.Value
        );
        return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
    }

    [Fact]
    public void 主库连接串按名称映射到对应的数据连接配置()
    {
        var dbConfig = new DbConfig();
        dbConfig.DataConnectionConfigs.Add(new DataConnectionConfig { Name = "default" });
        var configuration = BuildConfiguration(
            ("girvs-db-default", "Server=aspire-host;database=demo;")
        );

        dbConfig.ApplyAspireConnectionStrings(configuration);

        Assert.Equal(
            "Server=aspire-host;database=demo;",
            dbConfig.DataConnectionConfigs.First().MasterDataConnectionString
        );
    }

    [Fact]
    public void 读库连接串按连续编号全部映射()
    {
        var dbConfig = new DbConfig();
        dbConfig.DataConnectionConfigs.Add(new DataConnectionConfig { Name = "default" });
        var configuration = BuildConfiguration(
            ("girvs-db-default-read-0", "Server=read0;"),
            ("girvs-db-default-read-1", "Server=read1;")
        );

        dbConfig.ApplyAspireConnectionStrings(configuration);

        var readList = dbConfig.DataConnectionConfigs.First().ReadDataConnectionString;
        Assert.Equal(2, readList.Count);
        Assert.Equal("Server=read0;", readList[0]);
        Assert.Equal("Server=read1;", readList[1]);
    }

    [Fact]
    public void 无对应连接串时保持配置原值()
    {
        var dbConfig = new DbConfig();
        dbConfig.DataConnectionConfigs.Add(
            new DataConnectionConfig { Name = "default", MasterDataConnectionString = "原值" }
        );
        var configuration = BuildConfiguration();

        dbConfig.ApplyAspireConnectionStrings(configuration);

        Assert.Equal("原值", dbConfig.DataConnectionConfigs.First().MasterDataConnectionString);
    }

    [Fact]
    public void 缓存连接串映射到分布式缓存配置()
    {
        var cacheConfig = new CacheConfig();
        var configuration = BuildConfiguration(("girvs-cache", "aspire-redis:6379"));

        cacheConfig.ApplyAspireConnectionString(configuration);

        Assert.Equal("aspire-redis:6379", cacheConfig.DistributedCacheConfig.ConnectionString);
    }

    [Fact]
    public void 缓存无对应连接串时保持配置原值()
    {
        var cacheConfig = new CacheConfig();
        cacheConfig.DistributedCacheConfig.ConnectionString = "原值";
        var configuration = BuildConfiguration();

        cacheConfig.ApplyAspireConnectionString(configuration);

        Assert.Equal("原值", cacheConfig.DistributedCacheConfig.ConnectionString);
    }

    [Fact]
    public void 事件总线数据库与Redis连接串分别映射()
    {
        var eventBusConfig = new EventBusConfig();
        var configuration = BuildConfiguration(
            ("girvs-eventbus-db", "Server=cap-db;"),
            ("girvs-eventbus-redis", "cap-redis:6379")
        );

        eventBusConfig.ApplyAspireConnectionStrings(configuration);

        Assert.Equal("Server=cap-db;", eventBusConfig.DbConnectionString);
        Assert.Equal("cap-redis:6379", eventBusConfig.RedisConfig.RedisConnectionString);
    }

    [Fact]
    public void RabbitMq的AMQP连接串解析到各字段()
    {
        var eventBusConfig = new EventBusConfig();
        var configuration = BuildConfiguration(
            ("girvs-eventbus-rabbitmq", "amqp://guest:secret@rabbit-host:5673/myvhost")
        );

        eventBusConfig.ApplyAspireConnectionStrings(configuration);

        Assert.Equal("rabbit-host", eventBusConfig.RabbitMqConfig.HostName);
        Assert.Equal(5673, eventBusConfig.RabbitMqConfig.Port);
        Assert.Equal("guest", eventBusConfig.RabbitMqConfig.UserName);
        Assert.Equal("secret", eventBusConfig.RabbitMqConfig.Password);
        Assert.Equal("myvhost", eventBusConfig.RabbitMqConfig.VirtualHost);
    }

    [Fact]
    public void RabbitMq的AMQP连接串无vhost路径时VirtualHost默认为斜杠()
    {
        // Aspire 注入的 AMQP URI 通常不带 vhost 路径，应按标准语义置为 "/"，
        // 覆盖 appsettings 的非默认原值（否则本地 broker 因 vhost 不匹配连接失败）
        var eventBusConfig = new EventBusConfig();
        eventBusConfig.RabbitMqConfig.VirtualHost = "zhuofan.wb";
        var configuration = BuildConfiguration(
            ("girvs-eventbus-rabbitmq", "amqp://guest:pass@rabbit-host:5672")
        );

        eventBusConfig.ApplyAspireConnectionStrings(configuration);

        Assert.Equal("/", eventBusConfig.RabbitMqConfig.VirtualHost);
    }

    [Fact]
    public void RabbitMq连接串非URI格式时按纯主机名处理()
    {
        var eventBusConfig = new EventBusConfig();
        var originalPort = eventBusConfig.RabbitMqConfig.Port;
        var configuration = BuildConfiguration(("girvs-eventbus-rabbitmq", "rabbit-host"));

        eventBusConfig.ApplyAspireConnectionStrings(configuration);

        Assert.Equal("rabbit-host", eventBusConfig.RabbitMqConfig.HostName);
        Assert.Equal(originalPort, eventBusConfig.RabbitMqConfig.Port);
    }

    [Fact]
    public void 事件总线无任何对应连接串时保持配置原值()
    {
        var eventBusConfig = new EventBusConfig();
        var originalDbConnectionString = eventBusConfig.DbConnectionString;
        var configuration = BuildConfiguration();

        eventBusConfig.ApplyAspireConnectionStrings(configuration);

        Assert.Equal(originalDbConnectionString, eventBusConfig.DbConnectionString);
    }
}
