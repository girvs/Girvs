using Girvs.Configuration.Resources;
using Girvs.EntityFrameworkCore.Configuration;
using Xunit;

namespace Girvs.EntityFrameworkCore.Tests;

public class DataConnectionConfigTests
{
    private const string MySqlTemplate =
        "Server=mysql;Port=3306;Database={0};User ID=root;Password=secret;Allow User Variables=True;";

    [Fact]
    public void 构建连接串_使用资源ConnectionString模板()
    {
        var config = CreateConfig();
        var resource = CreateResource(
            "mysql",
            "Server=mysql;Database=sample;Allow User Variables=True;"
        );

        var connectionString = config.BuildConnectionString(resource);

        Assert.Equal("Server=mysql;Database=sample;Allow User Variables=True;", connectionString);
    }

    [Fact]
    public void 构建连接串_数组参数格式化ConnectionString()
    {
        var config = CreateConfig(["db", "sample"]);
        var resource = CreateResource("mysql", MySqlTemplate);

        var connectionString = config.BuildConnectionString(resource);

        Assert.Equal(
            "Server=mysql;Port=3306;Database=sample;User ID=root;Password=secret;Allow User Variables=True;",
            connectionString
        );
    }

    [Fact]
    public void 构建连接串_不支持的资源类型_抛出异常()
    {
        var config = CreateConfig();
        var resource = CreateResource("oracle", "Data Source=oracle;");

        var exception = Assert.Throws<GirvsException>(() => config.BuildConnectionString(resource));

        Assert.Contains("不支持 EntityFrameworkCore", exception.Message);
    }

    [Fact]
    public void 构建连接串_ConnectionString未配置_抛出可识别异常()
    {
        var config = CreateConfig();
        var resource = new GirvsInfrastructureResource
        {
            Type = "mysql",
            Settings = new Dictionary<string, string>(),
        };

        var exception = Assert.Throws<GirvsException>(() => config.BuildConnectionString(resource));

        Assert.Contains("Settings:ConnectionString 未配置", exception.Message);
    }

    [Fact]
    public void 解析连接串_支持主库与读库且各自带参数()
    {
        var config = CreateConfig(["db-{0}", "master"]);
        config.ReadConnectionRefs = [["db-read-{0}", "1"]];
        var resources = new Dictionary<string, GirvsInfrastructureResource>
        {
            ["db-master"] = CreateResource("mysql", "Server=master;Database={0};"),
            ["db-read-1"] = CreateResource("mysql", "Server=replica;Database={0};"),
        };

        config.ResolveConnectionStrings(resources);

        Assert.Equal("Server=master;Database=master;", config.GetMasterDataConnectionString());
        Assert.Equal(
            "Server=replica;Database=1;",
            config.GetSecureRandomReadDataConnectionString()
        );
    }

    [Fact]
    public void 解析连接串_未配置读库时读连接回退到主库()
    {
        var config = CreateConfig(["db"]);
        var resources = new Dictionary<string, GirvsInfrastructureResource>
        {
            ["db"] = CreateResource("mysql", "Server=master;Database=girvs;"),
        };

        config.ResolveConnectionStrings(resources);

        Assert.Equal(
            config.GetMasterDataConnectionString(),
            config.GetSecureRandomReadDataConnectionString()
        );
    }

    [Fact]
    public void 解析连接串_资源不存在_抛出可识别异常()
    {
        var config = CreateConfig(["missing"]);

        var exception = Assert.Throws<GirvsException>(() =>
            config.ResolveConnectionStrings(new Dictionary<string, GirvsInfrastructureResource>())
        );

        Assert.Contains("Resources:missing 未配置", exception.Message);
    }

    private static DataConnectionConfig CreateConfig(string[] connectionRef = null) =>
        new() { Name = "girvs", ConnectionRef = connectionRef ?? ["db"] };

    private static GirvsInfrastructureResource CreateResource(
        string type,
        string connectionString
    ) =>
        new()
        {
            Type = type,
            Settings = new Dictionary<string, string> {["ConnectionString"] = connectionString},
        };
}
