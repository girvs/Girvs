namespace Girvs.Aspire.Configuration;

/// <summary>
/// 将 Aspire AppHost 注入的 ConnectionStrings__* 环境变量映射到 Girvs 各模块配置节。
/// 通过 AppSettings.ModuleConfigurations 字典 + 反射按属性名写入，
/// 避免对 EntityFrameworkCore/Cache/EventBus 模块产生硬引用。
/// </summary>
public static class AspireConnectionStringMapper
{
    public static void Apply(IConfiguration configuration, AppSettings appSettings)
    {
        if (configuration == null || appSettings?.ModuleConfigurations == null)
            return;

        MapDbConfig(configuration, appSettings);
        MapCacheConfig(configuration, appSettings);
        MapEventBusConfig(configuration, appSettings);
    }

    private static void MapDbConfig(IConfiguration configuration, AppSettings appSettings)
    {
        if (!appSettings.ModuleConfigurations.TryGetValue("DbConfig", out var dbConfig))
            return;

        if (GetPropertyValue(dbConfig, "DataConnectionConfigs") is not IEnumerable connectionConfigs)
            return;

        foreach (var connectionConfig in connectionConfigs)
        {
            if (GetPropertyValue(connectionConfig, "Name") is not string name || name.Length == 0)
                continue;

            var master = configuration.GetConnectionString($"girvs-db-{name}");
            if (!string.IsNullOrEmpty(master))
                SetPropertyValue(connectionConfig, "MasterDataConnectionString", master);

            var readConnections = new List<string>();
            for (var i = 0; ; i++)
            {
                var read = configuration.GetConnectionString($"girvs-db-{name}-read-{i}");
                if (string.IsNullOrEmpty(read))
                    break;
                readConnections.Add(read);
            }

            if (readConnections.Count > 0)
                SetPropertyValue(connectionConfig, "ReadDataConnectionString", readConnections);
        }
    }

    private static void MapCacheConfig(IConfiguration configuration, AppSettings appSettings)
    {
        var redisConnection = configuration.GetConnectionString("girvs-cache");
        if (string.IsNullOrEmpty(redisConnection))
            return;

        if (!appSettings.ModuleConfigurations.TryGetValue("CacheConfig", out var cacheConfig))
            return;

        var distributedCacheConfig = GetPropertyValue(cacheConfig, "DistributedCacheConfig");
        if (distributedCacheConfig != null)
            SetPropertyValue(distributedCacheConfig, "ConnectionString", redisConnection);
    }

    private static void MapEventBusConfig(IConfiguration configuration, AppSettings appSettings)
    {
        if (!appSettings.ModuleConfigurations.TryGetValue("EventBusConfig", out var eventBusConfig))
            return;

        var dbConnection = configuration.GetConnectionString("girvs-eventbus-db");
        if (!string.IsNullOrEmpty(dbConnection))
            SetPropertyValue(eventBusConfig, "DbConnectionString", dbConnection);

        var redisConnection = configuration.GetConnectionString("girvs-eventbus-redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            var redisConfig = GetPropertyValue(eventBusConfig, "RedisConfig");
            if (redisConfig != null)
                SetPropertyValue(redisConfig, "RedisConnectionString", redisConnection);
        }

        var amqpUri = configuration.GetConnectionString("girvs-eventbus-rabbitmq");
        if (!string.IsNullOrEmpty(amqpUri))
        {
            var rabbitMqConfig = GetPropertyValue(eventBusConfig, "RabbitMqConfig");
            if (rabbitMqConfig != null)
                MapRabbitMq(rabbitMqConfig, amqpUri);
        }
    }

    private static void MapRabbitMq(object rabbitMqConfig, string amqpUri)
    {
        if (
            !Uri.TryCreate(amqpUri, UriKind.Absolute, out var uri)
            || !uri.Scheme.StartsWith("amqp", StringComparison.OrdinalIgnoreCase)
        )
        {
            // 非 AMQP URI 格式时视为纯主机名，其余字段保持 appsettings 原值
            SetPropertyValue(rabbitMqConfig, "HostName", amqpUri);
            return;
        }

        SetPropertyValue(rabbitMqConfig, "HostName", uri.Host);
        if (uri.Port > 0)
            SetPropertyValue(rabbitMqConfig, "Port", uri.Port);

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            var userInfo = uri.UserInfo.Split(':', 2);
            if (userInfo[0].Length > 0)
                SetPropertyValue(rabbitMqConfig, "UserName", Uri.UnescapeDataString(userInfo[0]));
            if (userInfo.Length == 2)
                SetPropertyValue(rabbitMqConfig, "Password", Uri.UnescapeDataString(userInfo[1]));
        }

        var virtualHost = uri.AbsolutePath.TrimStart('/');
        if (virtualHost.Length > 0)
            SetPropertyValue(rabbitMqConfig, "VirtualHost", Uri.UnescapeDataString(virtualHost));
    }

    private static object GetPropertyValue(object target, string propertyName) =>
        target?.GetType().GetProperty(propertyName)?.GetValue(target);

    private static void SetPropertyValue(object target, string propertyName, object value)
    {
        var property = target?.GetType().GetProperty(propertyName);
        if (property is { CanWrite: true })
            property.SetValue(target, value);
    }
}
