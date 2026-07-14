using Girvs.Cache.Configuration;
using Girvs.EntityFrameworkCore.Configuration;
using Girvs.EventBus.Configuration;

namespace Girvs.Aspire.Tests;

public class AspireConnectionStringMapperTests
{
    private static AppSettings CreateAppSettings(params IConfig[] configs)
    {
        var appSettings = new AppSettings();
        appSettings.PreLoadModelConfig();
        foreach (var config in configs)
        {
            appSettings.ModuleConfigurations.Add(config.GetType().Name, config);
        }

        return appSettings;
    }

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
        var appSettings = CreateAppSettings(dbConfig);
        var configuration = BuildConfiguration(
            ("girvs-db-default", "Server=aspire-host;database=demo;")
        );

        AspireConnectionStringMapper.Apply(configuration, appSettings);

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
        var appSettings = CreateAppSettings(dbConfig);
        var configuration = BuildConfiguration(
            ("girvs-db-default-read-0", "Server=read0;"),
            ("girvs-db-default-read-1", "Server=read1;")
        );

        AspireConnectionStringMapper.Apply(configuration, appSettings);

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
        var appSettings = CreateAppSettings(dbConfig);
        var configuration = BuildConfiguration();

        AspireConnectionStringMapper.Apply(configuration, appSettings);

        Assert.Equal("原值", dbConfig.DataConnectionConfigs.First().MasterDataConnectionString);
    }

    [Fact]
    public void 缓存连接串映射到分布式缓存配置()
    {
        var cacheConfig = new CacheConfig();
        var appSettings = CreateAppSettings(cacheConfig);
        var configuration = BuildConfiguration(("girvs-cache", "aspire-redis:6379"));

        AspireConnectionStringMapper.Apply(configuration, appSettings);

        Assert.Equal("aspire-redis:6379", cacheConfig.DistributedCacheConfig.ConnectionString);
    }

    [Fact]
    public void 事件总线数据库与Redis连接串分别映射()
    {
        var eventBusConfig = new EventBusConfig();
        var appSettings = CreateAppSettings(eventBusConfig);
        var configuration = BuildConfiguration(
            ("girvs-eventbus-db", "Server=cap-db;"),
            ("girvs-eventbus-redis", "cap-redis:6379")
        );

        AspireConnectionStringMapper.Apply(configuration, appSettings);

        Assert.Equal("Server=cap-db;", eventBusConfig.DbConnectionString);
        Assert.Equal("cap-redis:6379", eventBusConfig.RedisConfig.RedisConnectionString);
    }

    [Fact]
    public void RabbitMq的AMQP连接串解析到各字段()
    {
        var eventBusConfig = new EventBusConfig();
        var appSettings = CreateAppSettings(eventBusConfig);
        var configuration = BuildConfiguration(
            ("girvs-eventbus-rabbitmq", "amqp://guest:secret@rabbit-host:5673/myvhost")
        );

        AspireConnectionStringMapper.Apply(configuration, appSettings);

        Assert.Equal("rabbit-host", eventBusConfig.RabbitMqConfig.HostName);
        Assert.Equal(5673, eventBusConfig.RabbitMqConfig.Port);
        Assert.Equal("guest", eventBusConfig.RabbitMqConfig.UserName);
        Assert.Equal("secret", eventBusConfig.RabbitMqConfig.Password);
        Assert.Equal("myvhost", eventBusConfig.RabbitMqConfig.VirtualHost);
    }

    [Fact]
    public void RabbitMq连接串非URI格式时按纯主机名处理()
    {
        var eventBusConfig = new EventBusConfig();
        var originalPort = eventBusConfig.RabbitMqConfig.Port;
        var appSettings = CreateAppSettings(eventBusConfig);
        var configuration = BuildConfiguration(("girvs-eventbus-rabbitmq", "rabbit-host"));

        AspireConnectionStringMapper.Apply(configuration, appSettings);

        Assert.Equal("rabbit-host", eventBusConfig.RabbitMqConfig.HostName);
        Assert.Equal(originalPort, eventBusConfig.RabbitMqConfig.Port);
    }

    [Fact]
    public void 模块配置不存在时不抛异常()
    {
        var appSettings = CreateAppSettings();
        var configuration = BuildConfiguration(
            ("girvs-db-default", "Server=x;"),
            ("girvs-cache", "y:6379")
        );

        var exception = Record.Exception(
            () => AspireConnectionStringMapper.Apply(configuration, appSettings)
        );

        Assert.Null(exception);
    }
}
