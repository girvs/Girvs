using System.Text;
using Girvs.Cache.Configuration;
using Girvs.EntityFrameworkCore.Configuration;
using Girvs.EventBus.Configuration;
using Microsoft.Extensions.Configuration;


namespace Girvs.Aspire.Hosting.Tests;

public class ModuleConnectionReferenceTests
{
    [Fact]
    public void CacheFromJson引用共享配置中的模板资源时绑定数组引用()
    {
        const string json = """
        {
          "ModuleConfigurations": {
            "CacheConfig": {
              "DistributedCacheConfig": {
                "Enabled": true,
                "ConnectionRef": ["common-redis", "2"]
              }
            }
          },
          "Resources": {
            "common-redis": {
              "Type": "redis",
              "Settings": {
                "Endpoints": "192.168.51.183:6379,ssl=False,defaultdatabase={0}"
              }
            }
          }
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var configuration = new ConfigurationBuilder().AddJsonStream(stream).Build();

        var cacheConfig = configuration
            .GetSection("ModuleConfigurations:CacheConfig")
            .Get<CacheConfig>();

        Assert.Equal(["common-redis", "2"], cacheConfig.DistributedCacheConfig.ConnectionRef);

        var resource = configuration
            .GetSection("Resources")
            .Get<Dictionary<string, GirvsInfrastructureResource>>()["common-redis"];

        Assert.Equal(
            "192.168.51.183:6379,ssl=False,defaultdatabase=2",
            InfrastructureResourceReferenceResolver.FormatValue(
                resource.Settings["Endpoints"],
                cacheConfig.DistributedCacheConfig.ConnectionRef
            )
        );
    }

    [Fact]
    public void EF主从库连接从资源模板组装()
    {
        var db = new DataConnectionConfig
        {
            Name = "Ailynx",
            ConnectionRef = ["ailynx"],
            ReadConnectionRefs = [["ailynx-read"]],
        };
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
                    ["ConnectionString"] =
                        "Server=mysql;Port=3306;Database=Wb_Ailynx;User ID=root;Password=secret;Allow User Variables=True;",
                },
            },
            ["ailynx-read"] = new GirvsInfrastructureResource
            {
                Type = "mysql",
                Settings = new Dictionary<string, string>
                {
                    ["Host"] = "mysql-read",
                    ["Database"] = "Wb_Ailynx",
                    ["ConnectionString"] =
                        "Server=mysql-read;Database=Wb_Ailynx;Allow User Variables=True;",
                },
            },
        };

        db.ResolveConnectionStrings(resources);

        Assert.Contains("Server=mysql", db.GetMasterDataConnectionString());
        Assert.Contains("Server=mysql-read", db.GetSecureRandomReadDataConnectionString());
        Assert.Contains("Allow User Variables=True", db.GetMasterDataConnectionString());
        Assert.Contains("Allow User Variables=True", db.GetSecureRandomReadDataConnectionString());
    }

    [Fact]
    public void EF未配置读库时读连接回退到主库()
    {
        var db = new DataConnectionConfig { Name = "Ailynx", ConnectionRef = ["ailynx"] };

        db.ResolveConnectionStrings(
            new Dictionary<string, GirvsInfrastructureResource>
            {
                ["ailynx"] = new GirvsInfrastructureResource
                {
                    Type = "mysql",
                    Settings = new Dictionary<string, string>
                    {
                        ["ConnectionString"] = "Server=mysql;Database=Wb_Ailynx;",
                    },
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
        var db = new DataConnectionConfig { Name = "Ailynx", ConnectionRef = ["missing"] };

        var exception = Assert.Throws<GirvsException>(() =>
            db.ResolveConnectionStrings(new Dictionary<string, GirvsInfrastructureResource>())
        );

        Assert.Contains("Resources:missing", exception.Message);
    }

    [Fact]
    public void EventBusFromJson引用共享配置中的模板资源时绑定数组引用()
    {
        const string json = """
        {
          "ModuleConfigurations": {
            "EventBusConfig": {
              "PersistenceConnectionRef": ["common-mysql", "girvs_event_test"],
              "TransportConnectionRef": ["common-redis", "1"]
            }
          },
          "Resources": {
            "common-mysql": {
              "Type": "mysql",
              "Settings": {
                "ConnectionString": "Server=192.168.51.183;Port=3306;Database={0};User=root;Password=secret;"
              }
            },
            "common-redis": {
              "Type": "redis",
              "Settings": {
                "Endpoints": "192.168.51.183:6379,ssl=False,defaultdatabase={0}"
              }
            }
          }
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var configuration = new ConfigurationBuilder().AddJsonStream(stream).Build();

        var eventBusConfig = configuration
            .GetSection("ModuleConfigurations:EventBusConfig")
            .Get<EventBusConfig>();

        Assert.Equal(["common-mysql", "girvs_event_test"], eventBusConfig.PersistenceConnectionRef);
        Assert.Equal(["common-redis", "1"], eventBusConfig.TransportConnectionRef);

        var resources = configuration
            .GetSection("Resources")
            .Get<Dictionary<string, GirvsInfrastructureResource>>();

        Assert.Equal(
            "Server=192.168.51.183;Port=3306;Database=girvs_event_test;User=root;Password=secret;",
            InfrastructureResourceReferenceResolver.FormatValue(
                resources["common-mysql"].Settings["ConnectionString"],
                eventBusConfig.PersistenceConnectionRef
            )
        );
        Assert.Equal(
            "192.168.51.183:6379,ssl=False,defaultdatabase=1",
            InfrastructureResourceReferenceResolver.FormatValue(
                resources["common-redis"].Settings["Endpoints"],
                eventBusConfig.TransportConnectionRef
            )
        );
    }
}
