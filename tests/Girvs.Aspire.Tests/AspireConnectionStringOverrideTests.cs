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
    public void Aspire主库连接串覆盖资源组装结果()
    {
        var dbConfig = new DataConnectionConfig { Name = "default", ConnectionRef = "db" };
        var configuration = BuildConfiguration(
            ("girvs-db-default", "Server=aspire-host;database=demo;")
        );
        var resources = new Dictionary<string, Girvs.Configuration.Resources.Resource>
        {
            ["db"] = new() { Type = "mysql", Settings = new() { ["Host"] = "mysql", ["Database"] = "demo" } },
        };

        var provider = new DataConnectionStringProvider([dbConfig], resources, configuration);

        Assert.Equal("Server=aspire-host;database=demo;", provider.GetMasterConnectionString("default"));
    }

    [Fact]
    public void 读库连接串按连续编号全部映射()
    {
        var dbConfig = new DataConnectionConfig { Name = "default", ConnectionRef = "db", ReadConnectionRefs = ["read-0", "read-1"] };
        var configuration = BuildConfiguration(
            ("girvs-db-default-read-0", "Server=read0;"),
            ("girvs-db-default-read-1", "Server=read1;")
        );

        var resources = new Dictionary<string, Girvs.Configuration.Resources.Resource>
        {
            ["db"] = new() { Type = "mysql", Settings = new() { ["Host"] = "mysql", ["Database"] = "demo" } },
            ["read-0"] = new() { Type = "mysql", Settings = new() { ["Host"] = "read0", ["Database"] = "demo" } },
            ["read-1"] = new() { Type = "mysql", Settings = new() { ["Host"] = "read1", ["Database"] = "demo" } },
        };
        var provider = new DataConnectionStringProvider([dbConfig], resources, configuration);

        Assert.Contains(provider.GetReadConnectionString("default"), new[] { "Server=read0;", "Server=read1;" });
    }

    [Fact]
    public void Aspire缓存连接串覆盖资源组装结果()
    {
        var cacheConfig = new CacheConfig
        {
            DistributedCacheConfig = new DistributedCacheConfig
            {
                ConnectionRef = "cache",
                DefaultDatabase = 2,
            },
        };
        var configuration = BuildConfiguration(("girvs-cache", "aspire-redis:6379"));
        var resource = new Girvs.Configuration.Resources.Resource
        {
            Type = "redis",
            Settings = new Dictionary<string, string> { ["Endpoints"] = "redis:6379" },
        };

        var connectionString = cacheConfig.GetConnectionString(resource, configuration);

        Assert.Equal("aspire-redis:6379", connectionString);
    }


}
