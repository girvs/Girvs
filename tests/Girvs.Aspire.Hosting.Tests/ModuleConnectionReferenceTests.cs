using Girvs.Cache.Configuration;
using Girvs.EntityFrameworkCore.Configuration;
using Girvs.EventBus.Configuration;


namespace Girvs.Aspire.Hosting.Tests;

public class ModuleConnectionReferenceTests
{
    [Fact]
    public void Cache连接引用Redis资源时应用逻辑库与前缀()
    {
        var cache = new DistributedCacheConfig { ConnectionRef = "cache" };

        cache.DefaultDatabase = 3;

        var connectionString = cache.BuildConnectionString(
            new GirvsInfrastructureResource
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
            new GirvsInfrastructureResource
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
        var resources = new Dictionary<string, GirvsInfrastructureResource>
        {
            ["ailynx"] = new GirvsInfrastructureResource
            {
                Type = "mysql",
                Settings = new Dictionary<string, string>
                {
                    ["Host"] = "mysql",
                    ["Database"] = "Wb_Ailynx",
                    ["Port"] = "3306",
                },
            },
            ["ailynx-read"] = new GirvsInfrastructureResource
            {
                Type = "mysql",
                Settings = new Dictionary<string, string>
                {
                    ["Host"] = "mysql-read",
                    ["Database"] = "Wb_Ailynx",
                },
            },
        };

        db.ResolveConnectionStrings(resources);

        Assert.Contains("Server=mysql", db.GetMasterDataConnectionString());
        Assert.Contains("Server=mysql-read", db.GetSecureRandomReadDataConnectionString());
        Assert.Contains("Allow User Variables=True", db.GetMasterDataConnectionString());
        Assert.Contains("Allow User Variables=True", db.GetSecureRandomReadDataConnectionString());
        Assert.Contains(
            "Server=mysql",
            eventBus.BuildPersistenceConnectionString(resources["ailynx"])
        );
    }

    [Fact]
    public void EF未配置读库时读连接回退到主库()
    {
        var db = new DataConnectionConfig { Name = "Ailynx", ConnectionRef = "ailynx" };

        db.ResolveConnectionStrings(
            new Dictionary<string, GirvsInfrastructureResource>
            {
                ["ailynx"] = new GirvsInfrastructureResource
                {
                    Type = "mysql",
                    Settings = new Dictionary<string, string> { ["Host"] = "mysql" },
                },
            }
        );

        Assert.Equal(
            db.GetMasterDataConnectionString(),
            db.GetSecureRandomReadDataConnectionString()
        );
    }

    [Fact]
    public void EF引用的资源不存在时立即抛出()
    {
        var db = new DataConnectionConfig { Name = "Ailynx", ConnectionRef = "missing" };

        var exception = Assert.Throws<GirvsException>(() =>
            db.ResolveConnectionStrings(new Dictionary<string, GirvsInfrastructureResource>())
        );

        Assert.Contains("Resources:missing", exception.Message);
    }

    [Fact]
    public void EventBus持久化连接引用SqlServer资源时组装连接串()
    {
        var eventBus = new EventBusConfig { PersistenceConnectionRef = "eventbus-db" };

        var connectionString = eventBus.BuildPersistenceConnectionString(
            new GirvsInfrastructureResource
            {
                Type = "sqlserver",
                Settings = new Dictionary<string, string>
                {
                    ["Host"] = "sqlserver",
                    ["Port"] = "1433",
                    ["Database"] = "eventbus",
                    ["UserName"] = "sa",
                    ["Password"] = "password",
                },
            }
        );

        Assert.Contains("Data Source=sqlserver,1433", connectionString);
        Assert.Contains("Initial Catalog=eventbus", connectionString);
        Assert.Contains("User ID=sa", connectionString);
        Assert.Contains("Password=password", connectionString);
    }

    [Fact]
    public void EventBus持久化连接未指定数据库名时使用默认库名()
    {
        var eventBus = new EventBusConfig { PersistenceConnectionRef = "eventbus-db" };

        var mySqlConnectionString = eventBus.BuildPersistenceConnectionString(
            new GirvsInfrastructureResource
            {
                Type = "mysql",
                Settings = new Dictionary<string, string> { ["Host"] = "mysql" },
            }
        );
        var sqlServerConnectionString = eventBus.BuildPersistenceConnectionString(
            new GirvsInfrastructureResource
            {
                Type = "sqlserver",
                Settings = new Dictionary<string, string> { ["Host"] = "sqlserver" },
            }
        );

        Assert.Contains("Database=Girvs_EventBus", mySqlConnectionString);
        Assert.Contains("Initial Catalog=Girvs_EventBus", sqlServerConnectionString);
    }

    [Fact]
    public void EventBus持久化连接引用Sqlite资源时组装连接串()
    {
        var eventBus = new EventBusConfig { PersistenceConnectionRef = "eventbus-sqlite" };

        var connectionString = eventBus.BuildPersistenceConnectionString(
            new GirvsInfrastructureResource
            {
                Type = "sqlite",
                Settings = new Dictionary<string, string> { ["DataSource"] = "eventbus.db" },
            }
        );

        Assert.Equal("Data Source=eventbus.db", connectionString);
    }

    [Fact]
    public void EventBusRedisTransport使用资源端点()
    {
        var resource = new GirvsInfrastructureResource
        {
            Type = "redis",
            Settings = new Dictionary<string, string> { ["Endpoints"] = "redis:6379" },
        };

        Assert.Equal("redis:6379", resource.Settings["Endpoints"]);
    }
}
