using GirvsResource = Girvs.Configuration.Resources.Resource;

namespace Girvs.Aspire.Hosting.Tests;

public class GirvsResourceEntryBuilderTests
{
    private static IDistributedApplicationBuilder CreateBuilder() =>
        DistributedApplication.CreateBuilder(
            new DistributedApplicationOptions { DisableDashboard = true });

    /// <summary>为资源的主 endpoint 手动设置分配结果,模拟运行期端口分配。</summary>
    private static void AllocateEndpoint(IResource resource, int port)
    {
        var endpoint = resource.Annotations.OfType<EndpointAnnotation>().First();
        endpoint.AllocatedEndpoint = new AllocatedEndpoint(endpoint, "localhost", port);
    }

    private static GirvsResourceAnnotation GetAnnotation(IResource resource) =>
        resource.Annotations.OfType<GirvsResourceAnnotation>().Single();

    [Fact]
    public async Task Redis资源生成Endpoints设置且密码附于Endpoints内()
    {
        var builder = CreateBuilder();
        // Aspire 13.x 的 AddRedis 默认自动生成密码参数,密码以 StackExchange.Redis 选项形式附在 Endpoints 内透传
        var redis = builder.AddRedis("platform-redis").AsGirvsResource();
        AllocateEndpoint(redis.Resource, 56379);

        var entry = await GirvsResourceEntryBuilder.BuildAsync(
            redis.Resource, GetAnnotation(redis.Resource), CancellationToken.None);

        Assert.Equal("platform-redis", entry.Key);
        Assert.Equal("redis", entry.Value.Type);
        Assert.StartsWith("localhost:56379", entry.Value.Settings["Endpoints"]);
        if (redis.Resource.PasswordParameter is not null)
            Assert.Contains(",password=", entry.Value.Settings["Endpoints"]);
    }

    [Fact]
    public async Task Type覆盖参数生效()
    {
        var builder = CreateBuilder();
        var redis = builder.AddRedis("sample-redis")
            .AsGirvsResource(type: "redis-synchronized-memory");
        AllocateEndpoint(redis.Resource, 56380);

        var entry = await GirvsResourceEntryBuilder.BuildAsync(
            redis.Resource, GetAnnotation(redis.Resource), CancellationToken.None);

        Assert.Equal("redis-synchronized-memory", entry.Value.Type);
    }

    [Fact]
    public async Task MySql数据库资源生成完整连接设置()
    {
        var builder = CreateBuilder();
        var password = builder.AddParameter("mysql-pwd", "p@ss", secret: true);
        var server = builder.AddMySql("mysql-server", password: password);
        var database = server.AddDatabase("sample-mysql", databaseName: "sample")
            .AsGirvsResource();
        AllocateEndpoint(server.Resource, 53306);

        var entry = await GirvsResourceEntryBuilder.BuildAsync(
            database.Resource, GetAnnotation(database.Resource), CancellationToken.None);

        Assert.Equal("sample-mysql", entry.Key);
        Assert.Equal("mysql", entry.Value.Type);
        Assert.Equal("localhost", entry.Value.Settings["Host"]);
        Assert.Equal("53306", entry.Value.Settings["Port"]);
        Assert.Equal("root", entry.Value.Settings["UserName"]);
        Assert.Equal("p@ss", entry.Value.Settings["Password"]);
        Assert.Equal("sample", entry.Value.Settings["Database"]);
    }

    [Fact]
    public async Task RabbitMQ资源生成HostName设置()
    {
        var builder = CreateBuilder();
        var user = builder.AddParameter("mq-user", "guest");
        var pwd = builder.AddParameter("mq-pwd", "guest", secret: true);
        var rabbit = builder.AddRabbitMQ("sample-mq", userName: user, password: pwd)
            .AsGirvsResource();
        AllocateEndpoint(rabbit.Resource, 55672);

        var entry = await GirvsResourceEntryBuilder.BuildAsync(
            rabbit.Resource, GetAnnotation(rabbit.Resource), CancellationToken.None);

        Assert.Equal("rabbitmq", entry.Value.Type);
        Assert.Equal("localhost", entry.Value.Settings["HostName"]);
        Assert.Equal("55672", entry.Value.Settings["Port"]);
        Assert.Equal("guest", entry.Value.Settings["UserName"]);
        Assert.Equal("guest", entry.Value.Settings["Password"]);
    }

    [Fact]
    public async Task 额外Settings覆盖生成值()
    {
        var builder = CreateBuilder();
        var redis = builder.AddRedis("platform-redis")
            .AsGirvsResource(settings: new Dictionary<string, string> { ["Ssl"] = "true" });
        AllocateEndpoint(redis.Resource, 56381);

        var entry = await GirvsResourceEntryBuilder.BuildAsync(
            redis.Resource, GetAnnotation(redis.Resource), CancellationToken.None);

        Assert.Equal("true", entry.Value.Settings["Ssl"]);
    }

    [Fact]
    public async Task 不支持的资源类型且未显式指定Type时抛异常()
    {
        var builder = CreateBuilder();
        var container = builder.AddContainer("unknown", "busybox").AsGirvsResource();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            GirvsResourceEntryBuilder.BuildAsync(
                container.Resource, GetAnnotation(container.Resource), CancellationToken.None));
    }

    /// <summary>自定义提供程序:定义即生效(反射自动发现),把名为 search 的容器识别为 elasticsearch。</summary>
    private sealed class ElasticsearchSettingsProvider : IGirvsResourceSettingsProvider
    {
        public Task<GirvsResource> TryBuildAsync(IResource resource, CancellationToken ct)
        {
            if (resource is not ContainerResource { Name: "search" })
                return Task.FromResult<GirvsResource>(null);
            return Task.FromResult(new GirvsResource
            {
                Type = "elasticsearch",
                Settings = new Dictionary<string, string> { ["Url"] = "http://localhost:9200" },
            });
        }
    }

    /// <summary>
    /// 自定义提供程序:仅接管名为 takeover-redis 的 RedisResource,
    /// 验证自动发现的自定义提供程序优先于内置预设(条件收窄避免影响其它用例)。
    /// </summary>
    private sealed class RedisTakeoverProvider : IGirvsResourceSettingsProvider
    {
        public Task<GirvsResource> TryBuildAsync(IResource resource, CancellationToken ct) =>
            Task.FromResult(resource is RedisResource { Name: "takeover-redis" }
                ? new GirvsResource
                {
                    Type = "redis-custom",
                    Settings = new Dictionary<string, string> { ["Endpoints"] = "custom:1" },
                }
                : null);
    }

    [Fact]
    public async Task 自定义提供程序定义即自动发现支持新资源类型()
    {
        var builder = CreateBuilder();
        var search = builder.AddContainer("search", "elasticsearch:8").AsGirvsResource();

        var entry = await GirvsResourceEntryBuilder.BuildAsync(
            search.Resource, GetAnnotation(search.Resource), CancellationToken.None);

        Assert.Equal("elasticsearch", entry.Value.Type);
        Assert.Equal("http://localhost:9200", entry.Value.Settings["Url"]);
    }

    [Fact]
    public async Task 自定义提供程序优先于内置预设()
    {
        var builder = CreateBuilder();
        var redis = builder.AddRedis("takeover-redis").AsGirvsResource();

        var entry = await GirvsResourceEntryBuilder.BuildAsync(
            redis.Resource, GetAnnotation(redis.Resource), CancellationToken.None);

        Assert.Equal("redis-custom", entry.Value.Type);
        Assert.Equal("custom:1", entry.Value.Settings["Endpoints"]);
    }
}
