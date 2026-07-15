using Girvs.Cache;
using Girvs.EntityFrameworkCore;
using Girvs.EventBus;

namespace Girvs.Aspire.Hosting.Tests;

public class GirvsProjectOrchestrationTests : IDisposable
{
    [DependsOn(typeof(GirvsCacheModule))]
    private class CacheOnlyModule;

    [DependsOn(typeof(EventBusModule))]
    private class EventBusOnlyModule;

    [DependsOn(typeof(GirvsEntityFrameworkCoreModule))]
    private class DbOnlyModule;

    private class NoResourceModule;

    private readonly string _tempRoot = Directory
        .CreateTempSubdirectory("girvs-aspire-hosting-tests")
        .FullName;

    public void Dispose() => Directory.Delete(_tempRoot, true);

    private string CreateServiceProjectDirectory(string name, string appSettingsJson = null)
    {
        var directory = Path.Combine(_tempRoot, name);
        Directory.CreateDirectory(directory);
        File.WriteAllText(
            Path.Combine(directory, $"{name}.csproj"),
            """<Project Sdk="Microsoft.NET.Sdk.Web"></Project>"""
        );
        if (appSettingsJson != null)
            File.WriteAllText(Path.Combine(directory, "appsettings.json"), appSettingsJson);
        return directory;
    }

    private static IDistributedApplicationBuilder CreateBuilder() =>
        DistributedApplication.CreateBuilder(
            new DistributedApplicationOptions { DisableDashboard = true }
        );

    // 发布模式 builder：ExecutionContext.IsPublishMode 为 true，触发各贡献器的外部连接串引用分支
    private static IDistributedApplicationBuilder CreatePublishBuilder() =>
        DistributedApplication.CreateBuilder(
            new DistributedApplicationOptions
            {
                DisableDashboard = true,
                Args = ["--operation", "publish", "--publisher", "manifest", "--output-path", "."]
            }
        );

    private static IResourceBuilder<ProjectResource> AddServiceProject(
        IDistributedApplicationBuilder builder,
        string name,
        string projectDirectory
    ) => builder.AddProject(name, Path.Combine(projectDirectory, $"{name}.csproj"));

    [Fact]
    public void 声明Cache模块_自动创建Redis资源()
    {
        var builder = CreateBuilder();
        var directory = CreateServiceProjectDirectory("order-api");
        var project = AddServiceProject(builder, "order-api", directory);

        builder.WireGirvsResources(project, directory, typeof(CacheOnlyModule));

        Assert.Single(builder.Resources.OfType<RedisResource>(), r => r.Name == "girvs-cache");
    }

    [Fact]
    public void 两个服务声明Cache模块_共享同一个Redis资源()
    {
        var builder = CreateBuilder();
        var directoryA = CreateServiceProjectDirectory("order-api");
        var directoryB = CreateServiceProjectDirectory("user-api");
        var projectA = AddServiceProject(builder, "order-api", directoryA);
        var projectB = AddServiceProject(builder, "user-api", directoryB);

        builder.WireGirvsResources(projectA, directoryA, typeof(CacheOnlyModule));
        builder.WireGirvsResources(projectB, directoryB, typeof(CacheOnlyModule));

        Assert.Single(builder.Resources.OfType<RedisResource>());
    }

    [Fact]
    public void EventBusType为RabbitMQ_创建RabbitMQ资源()
    {
        var builder = CreateBuilder();
        var directory = CreateServiceProjectDirectory(
            "order-api",
            """
            {
              "ModuleConfigurations": {
                "EventBusConfig": { "EventBusType": "RabbitMQ" }
              }
            }
            """
        );
        var project = AddServiceProject(builder, "order-api", directory);

        builder.WireGirvsResources(project, directory, typeof(EventBusOnlyModule));

        Assert.Single(
            builder.Resources.OfType<RabbitMQServerResource>(),
            r => r.Name == "girvs-eventbus-rabbitmq"
        );
    }

    [Fact]
    public void EventBusType为数字0_同样创建RabbitMQ资源()
    {
        // 服务端框架会把枚举持久化为数值（RabbitMQ=0），贡献器须兼容数字与名称两种形式
        var builder = CreateBuilder();
        var directory = CreateServiceProjectDirectory(
            "order-api",
            """
            {
              "ModuleConfigurations": {
                "EventBusConfig": { "EventBusType": 0 }
              }
            }
            """
        );
        var project = AddServiceProject(builder, "order-api", directory);

        builder.WireGirvsResources(project, directory, typeof(EventBusOnlyModule));

        Assert.Single(
            builder.Resources.OfType<RabbitMQServerResource>(),
            r => r.Name == "girvs-eventbus-rabbitmq"
        );
    }

    [Fact]
    public void UseDataType为数字1_同样创建MySql数据库资源()
    {
        // 服务端框架会把枚举持久化为数值（MySql=1），贡献器须兼容数字与名称两种形式
        var builder = CreateBuilder();
        var directory = CreateServiceProjectDirectory(
            "order-api",
            """
            {
              "ModuleConfigurations": {
                "DbConfig": {
                  "DataConnectionConfigs": [ { "Name": "default", "UseDataType": 1 } ]
                }
              }
            }
            """
        );
        var project = AddServiceProject(builder, "order-api", directory);

        builder.WireGirvsResources(project, directory, typeof(DbOnlyModule));

        Assert.Single(builder.Resources.OfType<MySqlServerResource>(), r => r.Name == "girvs-mysql");
    }

    [Fact]
    public void EventBus配置缺失_不创建资源且不抛异常()
    {
        var builder = CreateBuilder();
        var directory = CreateServiceProjectDirectory("order-api");
        var project = AddServiceProject(builder, "order-api", directory);

        var exception = Record.Exception(
            () => builder.WireGirvsResources(project, directory, typeof(EventBusOnlyModule))
        );

        Assert.Null(exception);
        Assert.Empty(builder.Resources.OfType<RabbitMQServerResource>());
    }

    [Fact]
    public void 多命名库_分别创建MySql与SqlServer数据库资源()
    {
        var builder = CreateBuilder();
        var directory = CreateServiceProjectDirectory(
            "order-api",
            """
            {
              "ModuleConfigurations": {
                "DbConfig": {
                  "DataConnectionConfigs": [
                    { "Name": "default", "UseDataType": "MySql" },
                    { "Name": "log", "UseDataType": "MsSql" }
                  ]
                }
              }
            }
            """
        );
        var project = AddServiceProject(builder, "order-api", directory);

        builder.WireGirvsResources(project, directory, typeof(DbOnlyModule));

        Assert.Single(builder.Resources.OfType<MySqlServerResource>(), r => r.Name == "girvs-mysql");
        Assert.Single(
            builder.Resources.OfType<SqlServerServerResource>(),
            r => r.Name == "girvs-sqlserver"
        );
        Assert.Contains(builder.Resources, r => r.Name == "girvs-db-order-api-default");
        Assert.Contains(builder.Resources, r => r.Name == "girvs-db-order-api-log");
    }

    [Fact]
    public void 两个服务各自声明数据库_创建各自独立的数据库资源并共享服务器()
    {
        var builder = CreateBuilder();
        const string appSettings = """
            {
              "ModuleConfigurations": {
                "DbConfig": {
                  "DataConnectionConfigs": [{ "Name": "default", "UseDataType": "MySql" }]
                }
              }
            }
            """;
        var directoryA = CreateServiceProjectDirectory("order-api", appSettings);
        var directoryB = CreateServiceProjectDirectory("user-api", appSettings);
        var projectA = AddServiceProject(builder, "order-api", directoryA);
        var projectB = AddServiceProject(builder, "user-api", directoryB);

        builder.WireGirvsResources(projectA, directoryA, typeof(DbOnlyModule));
        builder.WireGirvsResources(projectB, directoryB, typeof(DbOnlyModule));

        Assert.Single(builder.Resources.OfType<MySqlServerResource>());
        Assert.Contains(builder.Resources, r => r.Name == "girvs-db-order-api-default");
        Assert.Contains(builder.Resources, r => r.Name == "girvs-db-user-api-default");
    }

    [Fact]
    public void 启用独立缓存_创建服务专属Redis资源()
    {
        var builder = CreateBuilder();
        var directory = CreateServiceProjectDirectory("order-api");
        var project = AddServiceProject(builder, "order-api", directory);

        builder.WireGirvsResources(
            project,
            directory,
            typeof(CacheOnlyModule),
            new GirvsProjectOptions { UseIsolatedCache = true }
        );

        Assert.Single(
            builder.Resources.OfType<RedisResource>(),
            r => r.Name == "girvs-cache-order-api"
        );
    }

    [Fact]
    public void 无资源映射的模块被忽略()
    {
        var builder = CreateBuilder();
        var directory = CreateServiceProjectDirectory("order-api");
        var project = AddServiceProject(builder, "order-api", directory);
        var resourceCountBefore = builder.Resources.Count;

        builder.WireGirvsResources(project, directory, typeof(NoResourceModule));

        Assert.Equal(resourceCountBefore, builder.Resources.Count);
    }

    // ---- 发布模式：基础设施改为外部连接串引用（阿里云托管），不建容器 ----

    [Fact]
    public void 发布模式Builder为IsPublishMode()
    {
        Assert.True(CreatePublishBuilder().ExecutionContext.IsPublishMode);
    }

    [Fact]
    public void 发布模式声明Cache模块_创建外部连接串引用而非Redis容器()
    {
        var builder = CreatePublishBuilder();
        var directory = CreateServiceProjectDirectory("order-api");
        var project = AddServiceProject(builder, "order-api", directory);

        builder.WireGirvsResources(project, directory, typeof(CacheOnlyModule));

        Assert.Empty(builder.Resources.OfType<RedisResource>());
        Assert.Contains(builder.Resources, r => r.Name == "girvs-cache");
    }

    [Fact]
    public void 发布模式EventBusRabbitMQ_创建外部连接串引用而非容器()
    {
        var builder = CreatePublishBuilder();
        var directory = CreateServiceProjectDirectory(
            "order-api",
            """{ "ModuleConfigurations": { "EventBusConfig": { "EventBusType": "RabbitMQ" } } }"""
        );
        var project = AddServiceProject(builder, "order-api", directory);

        builder.WireGirvsResources(project, directory, typeof(EventBusOnlyModule));

        Assert.Empty(builder.Resources.OfType<RabbitMQServerResource>());
        Assert.Contains(builder.Resources, r => r.Name == "girvs-eventbus-rabbitmq");
    }

    [Fact]
    public void 发布模式数据库_创建外部连接串引用而非容器()
    {
        var builder = CreatePublishBuilder();
        var directory = CreateServiceProjectDirectory(
            "order-api",
            """
            {
              "ModuleConfigurations": {
                "DbConfig": {
                  "DataConnectionConfigs": [ { "Name": "default", "UseDataType": "MySql" } ]
                }
              }
            }
            """
        );
        var project = AddServiceProject(builder, "order-api", directory);

        builder.WireGirvsResources(project, directory, typeof(DbOnlyModule));

        Assert.Empty(builder.Resources.OfType<MySqlServerResource>());
        Assert.Contains(builder.Resources, r => r.Name == "girvs-db-order-api-default");
    }
}
