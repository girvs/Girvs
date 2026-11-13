using Girvs.Configuration.Resources;
using Girvs.EntityFrameworkCore.Configuration;
using Xunit;

namespace Girvs.EntityFrameworkCore.Tests;

public class DataConnectionConfigTests
{
    [Fact]
    public void 构建连接串_MySQL资源_保留原有键与允许用户变量()
    {
        var config = CreateConfig();
        var resource = CreateResource(
            "mysql",
            new Dictionary<string, string>
            {
                ["Host"] = "mysql",
                ["Port"] = "3306",
                ["Database"] = "sample",
                ["UserName"] = "root",
                ["Password"] = "secret",
            }
        );

        var connectionString = config.BuildConnectionString(resource);

        Assert.Contains("Server=mysql", connectionString);
        Assert.Contains("Port=3306", connectionString);
        Assert.Contains("Database=sample", connectionString);
        Assert.Contains("User ID=root", connectionString);
        Assert.Contains("Password=secret", connectionString);
        Assert.Contains("Allow User Variables=True", connectionString);
    }

    [Fact]
    public void 构建连接串_SQLServer资源_端口并入数据源()
    {
        var config = CreateConfig();
        var resource = CreateResource(
            "sqlserver",
            new Dictionary<string, string>
            {
                ["Host"] = "sqlserver",
                ["Port"] = "1433",
                ["Database"] = "sample",
                ["UserName"] = "sa",
                ["Password"] = "secret",
            }
        );

        var connectionString = config.BuildConnectionString(resource);

        Assert.Contains("Data Source=sqlserver,1433", connectionString);
        Assert.Contains("Initial Catalog=sample", connectionString);
        Assert.Contains("User ID=sa", connectionString);
        Assert.Contains("Password=secret", connectionString);
        Assert.Contains("TrustServerCertificate=True", connectionString);
    }

    [Fact]
    public void 构建连接串_SQLServer未指定数据库和端口_回退配置名称且不拼端口()
    {
        var config = CreateConfig();
        var resource = CreateResource(
            "sqlserver",
            new Dictionary<string, string> { ["Host"] = "sqlserver" }
        );

        var connectionString = config.BuildConnectionString(resource);

        Assert.Contains("Data Source=sqlserver", connectionString);
        Assert.Contains("Initial Catalog=girvs", connectionString);
        Assert.DoesNotContain("sqlserver,", connectionString);
    }

    [Fact]
    public void 构建连接串_SQLServer可显式关闭证书信任()
    {
        var config = CreateConfig();
        var resource = CreateResource(
            "sqlserver",
            new Dictionary<string, string>
            {
                ["Host"] = "sqlserver",
                ["TrustServerCertificate"] = "false",
            }
        );

        var connectionString = config.BuildConnectionString(resource);

        Assert.Contains("TrustServerCertificate=False", connectionString);
    }

    [Fact]
    public void 构建连接串_不支持的资源类型_抛出异常()
    {
        var config = CreateConfig();
        var resource = CreateResource(
            "oracle",
            new Dictionary<string, string> { ["Host"] = "oracle" }
        );

        var exception = Assert.Throws<GirvsException>(() => config.BuildConnectionString(resource));

        Assert.Contains("不支持 EntityFrameworkCore", exception.Message);
    }

    [Fact]
    public void 解析连接串_支持SQLServer主库与读库()
    {
        var config = CreateConfig();
        config.ReadConnectionRefs = ["db-read"];
        var resources = new Dictionary<string, GirvsInfrastructureResource>
        {
            ["db"] = CreateResource(
                "sqlserver",
                new Dictionary<string, string> { ["Host"] = "master", ["Database"] = "girvs" }
            ),
            ["db-read"] = CreateResource(
                "sqlserver",
                new Dictionary<string, string> { ["Host"] = "replica", ["Database"] = "girvs" }
            ),
        };

        config.ResolveConnectionStrings(resources);

        Assert.Contains("Data Source=master", config.GetMasterDataConnectionString());
        Assert.Contains("Data Source=replica", config.GetSecureRandomReadDataConnectionString());
    }

    private static DataConnectionConfig CreateConfig() =>
        new() { Name = "girvs", ConnectionRef = "db" };

    private static GirvsInfrastructureResource CreateResource(
        string type,
        Dictionary<string, string> settings
    ) => new() { Type = type, Settings = settings };
}
