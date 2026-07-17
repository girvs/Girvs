using Girvs.Configuration.Resources;
using Girvs.Cache.Configuration;
using Girvs.EntityFrameworkCore.Configuration;
using Girvs.EventBus.Configuration;
using GirvsResource = Girvs.Configuration.Resources.Resource;

namespace Girvs.Aspire.Hosting.Tests;

public class ModuleConnectionReferenceTests
{
    [Fact]
    public void Cache连接引用Redis资源时应用逻辑库与前缀()
    {
        var cache = new DistributedCacheConfig { ConnectionRef = "cache" };

        cache.DefaultDatabase = 3;

        var connectionString = cache.BuildConnectionString(
            new GirvsResource
            {
                Type = "redis",
                Settings = new Dictionary<string, string>
                {
                    ["Endpoints"] = "redis:6379",
                },
            }
        );

        Assert.Equal("redis:6379,defaultDatabase=3", connectionString);
    }

    [Fact]
    public void Cache连接引用SqlServer资源时组装连接串()
    {
        var cache = new DistributedCacheConfig { ConnectionRef = "cache-db" };

        var connectionString = cache.BuildConnectionString(
            new GirvsResource
            {
                Type = "sqlserver",
                Settings = new Dictionary<string, string>
                {
                    ["Host"] = "sqlserver",
                    ["Database"] = "cache",
                    ["UserName"] = "sa",
                    ["Password"] = "password",
                },
            }
        );

        Assert.Contains("Data Source=sqlserver", connectionString);
        Assert.Contains("Initial Catalog=cache", connectionString);
    }

    [Fact]
    public void EF主从库与EventBus持久化连接均从资源组装()
    {
        var db = new DataConnectionConfig
        {
            Name = "Ailynx",
            ConnectionRef = "ailynx",
            ReadConnectionRefs = ["ailynx-read"],
        };
        var eventBus = new EventBusConfig { PersistenceConnectionRef = "ailynx" };
        var resources = new Dictionary<string, GirvsResource>
        {
            ["ailynx"] = new GirvsResource
            {
                Type = "mysql",
                Settings = new Dictionary<string, string>
                {
                    ["Host"] = "mysql",
                    ["Database"] = "Wb_Ailynx",
                    ["Port"] = "3306",
                },
            },
            ["ailynx-read"] = new GirvsResource
            {
                Type = "mysql",
                Settings = new Dictionary<string, string>
                {
                    ["Host"] = "mysql-read",
                    ["Database"] = "Wb_Ailynx",
                },
            },
        };

        var provider = new DataConnectionStringProvider([db], resources, null);

        Assert.Contains("Server=mysql", provider.GetMasterConnectionString("Ailynx"));
        Assert.Contains("Server=mysql-read", provider.GetReadConnectionString("Ailynx"));
        Assert.Equal(
            provider.GetMasterConnectionString("Ailynx"),
            eventBus.BuildPersistenceConnectionString(resources["ailynx"])
        );
    }

    [Fact]
    public void EventBusRedisTransport使用资源端点()
    {
        var resource = new GirvsResource
        {
            Type = "redis",
            Settings = new Dictionary<string, string> { ["Endpoints"] = "redis:6379" },
        };

        Assert.Equal("redis:6379", resource.Settings["Endpoints"]);
    }
}
