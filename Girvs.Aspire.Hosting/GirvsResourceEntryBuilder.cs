using GirvsResource = Girvs.Configuration.Resources.Resource;

namespace Girvs.Aspire.Hosting;

/// <summary>
/// 把已登记的 Aspire 资源转换为 Girvs Resource 条目。
/// Settings 键与各模块 BuildConnectionString 的消费约定对齐(见设计文档表格)。
/// </summary>
internal static class GirvsResourceEntryBuilder
{
    public static async Task<KeyValuePair<string, GirvsResource>> BuildAsync(
        IResource resource,
        GirvsResourceAnnotation annotation,
        CancellationToken ct
    )
    {
        var (type, settings) = resource switch
        {
            RedisResource redis => ("redis", await RedisSettingsAsync(redis, ct)),
            MySqlDatabaseResource mysqlDb => (
                "mysql",
                await MySqlSettingsAsync(mysqlDb.Parent, mysqlDb.DatabaseName, ct)
            ),
            MySqlServerResource mysql => ("mysql", await MySqlSettingsAsync(mysql, null, ct)),
            SqlServerDatabaseResource sqlDb => (
                "sqlserver",
                await SqlServerSettingsAsync(sqlDb.Parent, sqlDb.DatabaseName, ct)
            ),
            SqlServerServerResource sql => (
                "sqlserver",
                await SqlServerSettingsAsync(sql, null, ct)
            ),
            RabbitMQServerResource rabbit => ("rabbitmq", await RabbitSettingsAsync(rabbit, ct)),
            KafkaServerResource kafka => ("kafka", KafkaSettings(kafka)),
            _ when annotation.TypeOverride is not null => (
                annotation.TypeOverride,
                new Dictionary<string, string>()
            ),
            _ => throw new InvalidOperationException(
                $"AsGirvsResource 不支持资源类型 {resource.GetType().Name},请显式指定 type 与 settings 参数"
            ),
        };

        // 显式参数覆盖自动生成的结果
        if (annotation.TypeOverride is not null)
            type = annotation.TypeOverride;
        foreach (var (key, value) in annotation.ExtraSettings)
            settings[key] = value;

        return new KeyValuePair<string, GirvsResource>(
            resource.Name,
            new GirvsResource { Type = type, Settings = settings }
        );
    }

    private static (string Host, int Port) PrimaryEndpoint(IResourceWithEndpoints resource)
    {
        var endpoint = resource.GetEndpoints().First();
        return (endpoint.Host, endpoint.Port);
    }

    private static async Task<Dictionary<string, string>> RedisSettingsAsync(
        RedisResource redis,
        CancellationToken ct
    )
    {
        var (host, port) = PrimaryEndpoint(redis);
        var endpoints = $"{host}:{port}";
        // DistributedCacheConfig.BuildRedisConnectionString 会把 Endpoints 按逗号拆分后原样拼回,
        // 所以密码以 StackExchange.Redis 选项形式附在 Endpoints 内即可透传,模块无需感知。
        if (redis.PasswordParameter is not null)
            endpoints += $",password={await redis.PasswordParameter.GetValueAsync(ct)}";
        return new Dictionary<string, string> { ["Endpoints"] = endpoints };
    }

    private static async Task<Dictionary<string, string>> MySqlSettingsAsync(
        MySqlServerResource mysql,
        string databaseName,
        CancellationToken ct
    )
    {
        var (host, port) = PrimaryEndpoint(mysql);
        var settings = new Dictionary<string, string>
        {
            ["Host"] = host,
            ["Port"] = port.ToString(),
            ["UserName"] = "root",
            ["Password"] = await mysql.PasswordParameter.GetValueAsync(ct),
        };
        if (databaseName is not null)
            settings["Database"] = databaseName;
        return settings;
    }

    private static async Task<Dictionary<string, string>> SqlServerSettingsAsync(
        SqlServerServerResource sql,
        string databaseName,
        CancellationToken ct
    )
    {
        var (host, port) = PrimaryEndpoint(sql);
        var settings = new Dictionary<string, string>
        {
            ["Host"] = host,
            ["Port"] = port.ToString(),
            ["UserName"] = "sa",
            ["Password"] = await sql.PasswordParameter.GetValueAsync(ct),
        };
        if (databaseName is not null)
            settings["Database"] = databaseName;
        return settings;
    }

    private static async Task<Dictionary<string, string>> RabbitSettingsAsync(
        RabbitMQServerResource rabbit,
        CancellationToken ct
    )
    {
        var (host, port) = PrimaryEndpoint(rabbit);
        return new Dictionary<string, string>
        {
            ["HostName"] = host,
            ["Port"] = port.ToString(),
            ["UserName"] = rabbit.UserNameParameter is null
                ? "guest"
                : await rabbit.UserNameParameter.GetValueAsync(ct),
            ["Password"] = await rabbit.PasswordParameter.GetValueAsync(ct),
            ["VirtualHost"] = "/",
        };
    }

    private static Dictionary<string, string> KafkaSettings(KafkaServerResource kafka)
    {
        var (host, port) = PrimaryEndpoint(kafka);
        return new Dictionary<string, string> { ["BootstrapServers"] = $"{host}:{port}" };
    }
}
