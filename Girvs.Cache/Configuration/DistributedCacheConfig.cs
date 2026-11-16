using Girvs.Configuration.Resources;

namespace Girvs.Cache.Configuration;

/// <summary>
/// Represents distributed cache configuration parameters
/// </summary>
public partial class DistributedCacheConfig
{
    /// <summary>
    /// 缓存资源引用：索引 0 为 Resources 资源键格式串，索引 1 起为 string.Format 参数；
    /// 同一组参数既用于组合资源键，也用于格式化资源的 Settings:Endpoints。
    /// 例如 ["common-redis", "0"] 会引用资源 common-redis，并把 Endpoints 中的 {0} 替换为 0。
    /// </summary>
    public string[] ConnectionRef { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether we should use distributed cache
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Gets or sets Redis logical database. Used when distributed cache is Redis or RedisSynchronizedMemory.
    /// </summary>
    public int DefaultDatabase { get; set; }

    /// <summary>
    /// Gets or sets schema name. Used when distributed cache is enabled and DistributedCacheType property is set as SqlServer
    /// </summary>
    public string SchemaName { get; set; } = "dbo";

    /// <summary>
    /// Gets or sets table name. Used when distributed cache is enabled and DistributedCacheType property is set as SqlServer
    /// </summary>
    public string TableName { get; set; } = "DistributedCache";

    /// <summary>
    /// Gets or sets instance name. Used when distributed cache is enabled and DistributedCacheType property is set as Redis or RedisSynchronizedMemory.
    /// Useful when one wants to partition a single Redis server for use with multiple apps, e.g. by setting InstanceName to "development" and "production".
    /// </summary>
    public string InstanceName { get; set; } = "girvs";

    /// <summary>
    /// Gets or sets the Redis event publish interval in milliseconds.
    /// Used when distributed cache is enabled and DistributedCacheType property is set as RedisSynchronizedMemory.
    /// If greater than zero, events will be buffered for this long before being published in batch, in order to reduce server load.
    /// If zero, events are published when they are raised, without buffering.
    /// </summary>
    public int PublishIntervalMs { get; set; } = 500;

    public string BuildConnectionString(GirvsInfrastructureResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        return (resource.Type ?? string.Empty).ToLowerInvariant() switch
        {
            // "sqlserver" => BuildSqlServerConnectionString(resource),
            "redis" or "redis-synchronized-memory" => BuildRedisConnectionString(resource),
            _ => throw new GirvsException(
                $"Resources:{InfrastructureResourceReferenceResolver.FormatResourceKey(ConnectionRef)}:Type 不支持缓存"
            ),
        };
    }

    // private string BuildSqlServerConnectionString(Resource resource)
    // {
    //     if (!string.Equals(resource.Type, "sqlserver", StringComparison.OrdinalIgnoreCase))
    //         throw new GirvsException($"Resources:{ConnectionRef}:Type 必须为 sqlserver");
    //     if (!resource.Settings.TryGetValue("Host", out var host) || string.IsNullOrWhiteSpace(host))
    //         throw new GirvsException($"Resources:{ConnectionRef}:Settings:Host 未配置");
    //     if (!resource.Settings.TryGetValue("Database", out var database) || string.IsNullOrWhiteSpace(database))
    //         throw new GirvsException($"Resources:{ConnectionRef}:Settings:Database 未配置");
    //
    //     var builder = new System.Data.Common.DbConnectionStringBuilder
    //     {
    //         ["Data Source"] = int.TryParse(resource.Settings.GetValueOrDefault("Port"), out var port) ? $"{host},{port}" : host,
    //         ["Initial Catalog"] = database,
    //     };
    //     if (resource.Settings.TryGetValue("UserName", out var userName) && !string.IsNullOrWhiteSpace(userName)) builder["User ID"] = userName;
    //     if (resource.Settings.TryGetValue("Password", out var password) && !string.IsNullOrWhiteSpace(password)) builder["Password"] = password;
    //     return builder.ConnectionString;
    // }

    private string BuildRedisConnectionString(GirvsInfrastructureResource resource)
    {
        var resourceKey = InfrastructureResourceReferenceResolver.FormatResourceKey(ConnectionRef);
        if (!resource.Settings.TryGetValue("Endpoints", out var endpoints) || string.IsNullOrWhiteSpace(endpoints))
            throw new GirvsException($"Resources:{resourceKey}:Settings:Endpoints 未配置");

        // Endpoints 支持与资源键相同的占位符参数，例如 common-redis 的 defaultdatabase={0}
        var formattedEndpoints = InfrastructureResourceReferenceResolver.FormatValue(endpoints, ConnectionRef);

        var parts = formattedEndpoints
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList();
        if (parts.Count == 0)
            throw new GirvsException($"Resources:{resourceKey}:Settings:Endpoints 未配置");
        if (
            resource.Settings.TryGetValue("Ssl", out var ssl)
            && bool.TryParse(ssl, out var useSsl)
            && useSsl
            && !parts.Any(part => part.StartsWith("ssl=", StringComparison.OrdinalIgnoreCase))
        )
            parts.Add("ssl=True");
        // Endpoints 已显式指定逻辑库时以模板为准，避免追加项覆盖引用参数
        if (!parts.Any(part => part.StartsWith("defaultdatabase=", StringComparison.OrdinalIgnoreCase)))
            parts.Add($"defaultDatabase={DefaultDatabase}");
        return string.Join(',', parts);
    }
}
