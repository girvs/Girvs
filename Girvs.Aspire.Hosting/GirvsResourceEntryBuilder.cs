using Girvs.TypeFinder;
using GirvsResource = Girvs.Configuration.Resources.Resource;

namespace Girvs.Aspire.Hosting;

/// <summary>
/// 把已登记的 Aspire 资源转换为 Girvs Resource 条目。
/// 匹配顺序:自动发现的自定义提供程序(实现 IGirvsResourceSettingsProvider 即生效)
/// → 内置预设(redis / mysql / sqlserver / rabbitmq / kafka)
/// → AsGirvsResource 显式指定的 type/settings 兜底。
/// Settings 键与各模块 BuildConnectionString 的消费约定对齐(见设计文档表格)。
/// </summary>
internal static class GirvsResourceEntryBuilder
{
    // 与框架模块机制同款的 TypeFinder 自动发现,内置预设与自定义提供程序走同一条路径:
    // 实现 IGirvsResourceSettingsProvider(公共无参构造)定义即生效,无需注册。
    // 用 AppDomainTypeFinder(WebAppTypeFinder 的 bin 目录扫描依赖 Web 宿主文件提供程序,
    // AppHost 进程没有),提供程序需定义在已加载的程序集——通常就是 AppHost 项目本身。
    // 排序:自定义(非本程序集)在前,可接管内置类型;再按类型全名排序保证确定性。
    private static readonly Lazy<IGirvsResourceSettingsProvider[]> Providers = new(() =>
        new AppDomainTypeFinder()
            .FindOfType<IGirvsResourceSettingsProvider>()
            .Where(type => type.GetConstructor(Type.EmptyTypes) is not null)
            .OrderBy(type => type.Assembly == typeof(GirvsResourceEntryBuilder).Assembly)
            .ThenBy(type => type.FullName, StringComparer.Ordinal)
            .Select(type => (IGirvsResourceSettingsProvider)Activator.CreateInstance(type))
            .ToArray()
    );

    public static async Task<KeyValuePair<string, GirvsResource>> BuildAsync(
        IResource resource,
        GirvsResourceAnnotation annotation,
        CancellationToken ct
    )
    {
        var built =
            await TryBuildFromProvidersAsync(Providers.Value, resource, ct)
            ?? (
                annotation.TypeOverride is not null
                    ? new GirvsResource { Type = annotation.TypeOverride }
                    : throw new InvalidOperationException(
                        $"AsGirvsResource 不支持资源类型 {resource.GetType().Name}:"
                            + "请实现 IGirvsResourceSettingsProvider(自动发现,无需注册),"
                            + "或显式指定 type 与 settings 参数"
                    )
            );

        // AsGirvsResource 显式参数覆盖提取结果
        if (annotation.TypeOverride is not null)
            built.Type = annotation.TypeOverride;
        foreach (var (key, value) in annotation.ExtraSettings)
            built.Settings[key] = value;

        return new KeyValuePair<string, GirvsResource>(resource.Name, built);
    }

    private static async Task<GirvsResource> TryBuildFromProvidersAsync(
        IReadOnlyList<IGirvsResourceSettingsProvider> providers,
        IResource resource,
        CancellationToken ct
    )
    {
        foreach (var provider in providers)
        {
            var built = await provider.TryBuildAsync(resource, ct);
            if (built is not null)
                return built;
        }

        return null;
    }

    private static (string Host, int Port) PrimaryEndpoint(IResourceWithEndpoints resource)
    {
        var endpoint = resource.GetEndpoints().First();
        return (endpoint.Host, endpoint.Port);
    }

    private sealed class RedisSettingsProvider : IGirvsResourceSettingsProvider
    {
        public async Task<GirvsResource> TryBuildAsync(IResource resource, CancellationToken ct)
        {
            if (resource is not RedisResource redis)
                return null;

            var (host, port) = PrimaryEndpoint(redis);
            var endpoints = $"{host}:{port}";
            // DistributedCacheConfig.BuildRedisConnectionString 会把 Endpoints 按逗号拆分后原样拼回,
            // 所以密码以 StackExchange.Redis 选项形式附在 Endpoints 内即可透传,模块无需感知。
            if (redis.PasswordParameter is not null)
                endpoints += $",password={await redis.PasswordParameter.GetValueAsync(ct)}";
            return new GirvsResource
            {
                Type = "redis",
                Settings = new Dictionary<string, string> { ["Endpoints"] = endpoints },
            };
        }
    }

    private sealed class MySqlSettingsProvider : IGirvsResourceSettingsProvider
    {
        public async Task<GirvsResource> TryBuildAsync(IResource resource, CancellationToken ct)
        {
            var (server, databaseName) = resource switch
            {
                MySqlDatabaseResource database => (database.Parent, database.DatabaseName),
                MySqlServerResource mysql => (mysql, null),
                _ => (null, null),
            };
            if (server is null)
                return null;

            var (host, port) = PrimaryEndpoint(server);
            var settings = new Dictionary<string, string>
            {
                ["Host"] = host,
                ["Port"] = port.ToString(),
                ["UserName"] = "root",
                ["Password"] = await server.PasswordParameter.GetValueAsync(ct),
            };
            if (databaseName is not null)
                settings["Database"] = databaseName;
            return new GirvsResource { Type = "mysql", Settings = settings };
        }
    }

    private sealed class SqlServerSettingsProvider : IGirvsResourceSettingsProvider
    {
        public async Task<GirvsResource> TryBuildAsync(IResource resource, CancellationToken ct)
        {
            var (server, databaseName) = resource switch
            {
                SqlServerDatabaseResource database => (database.Parent, database.DatabaseName),
                SqlServerServerResource sql => (sql, null),
                _ => (null, null),
            };
            if (server is null)
                return null;

            var (host, port) = PrimaryEndpoint(server);
            var settings = new Dictionary<string, string>
            {
                ["Host"] = host,
                ["Port"] = port.ToString(),
                ["UserName"] = "sa",
                ["Password"] = await server.PasswordParameter.GetValueAsync(ct),
            };
            if (databaseName is not null)
                settings["Database"] = databaseName;
            return new GirvsResource { Type = "sqlserver", Settings = settings };
        }
    }

    private sealed class RabbitMQSettingsProvider : IGirvsResourceSettingsProvider
    {
        public async Task<GirvsResource> TryBuildAsync(IResource resource, CancellationToken ct)
        {
            if (resource is not RabbitMQServerResource rabbit)
                return null;

            var (host, port) = PrimaryEndpoint(rabbit);
            return new GirvsResource
            {
                Type = "rabbitmq",
                Settings = new Dictionary<string, string>
                {
                    ["HostName"] = host,
                    ["Port"] = port.ToString(),
                    ["UserName"] = rabbit.UserNameParameter is null
                        ? "guest"
                        : await rabbit.UserNameParameter.GetValueAsync(ct),
                    ["Password"] = await rabbit.PasswordParameter.GetValueAsync(ct),
                    ["VirtualHost"] = "/",
                },
            };
        }
    }

    private sealed class KafkaSettingsProvider : IGirvsResourceSettingsProvider
    {
        public Task<GirvsResource> TryBuildAsync(IResource resource, CancellationToken ct)
        {
            if (resource is not KafkaServerResource kafka)
                return Task.FromResult<GirvsResource>(null);

            var (host, port) = PrimaryEndpoint(kafka);
            return Task.FromResult(
                new GirvsResource
                {
                    Type = "kafka",
                    Settings = new Dictionary<string, string>
                    {
                        ["BootstrapServers"] = $"{host}:{port}",
                    },
                }
            );
        }
    }
}
