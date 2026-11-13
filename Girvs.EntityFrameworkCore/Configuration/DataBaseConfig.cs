using System.Data.Common;
using Girvs.Configuration.Resources;

namespace Girvs.EntityFrameworkCore.Configuration;

public class DbConfig : IAppModuleConfig
{
    public ICollection<DataConnectionConfig> DataConnectionConfigs { get; set; } =
        new List<DataConnectionConfig>();

    public void Init()
    {
        DataConnectionConfigs.Add(new DataConnectionConfig());
    }

    public DataConnectionConfig GetDataConnectionConfig<TDbContext>()
        where TDbContext : GirvsDbContext
    {
        return GetDataConnectionConfig(typeof(TDbContext));
    }

    public DataConnectionConfig GetDataConnectionConfig(Type dbContextType)
    {
        var dbConfigAttribute = dbContextType.GetCustomAttribute<GirvsDbConfigAttribute>();

        if (dbConfigAttribute == null)
        {
            throw new GirvsException($"{dbContextType.Name} 未绑定指定的数据库配置");
        }

        var dataBaseConfig = DataConnectionConfigs.FirstOrDefault(x =>
            x.Name == dbConfigAttribute.DbName
        );

        if (dataBaseConfig == null)
        {
            throw new GirvsException(
                $"{dbContextType.Name} 绑定指定的数据库配置不正确 {dbConfigAttribute.DbName}"
            );
        }

        return dataBaseConfig;
    }

    /// <summary>
    /// 为所有数据连接配置解析主库/读库连接串。应在模块注册时调用一次，缺失资源会立即抛出。
    /// </summary>
    public void ResolveConnectionStrings(
        IReadOnlyDictionary<string, GirvsInfrastructureResource> resources
    )
    {
        foreach (var connectionConfig in DataConnectionConfigs)
            connectionConfig.ResolveConnectionStrings(resources);
    }
}

public class DataConnectionConfig
{
    /// <summary>已解析的连接串。整体替换，避免懒解析并发时读到半成品。</summary>
    private sealed record ResolvedConnections(string Master, IReadOnlyList<string> Reads);

    private ResolvedConnections _connections;

    /// <summary>引用 Resources 中的数据库资源键（支持 mysql / sqlserver）。</summary>
    public string ConnectionRef { get; set; }

    /// <summary>
    /// 数据库名称
    /// </summary>
    public string Name { get; set; } = "default";

    /// <summary>
    /// 启用自动还原数据库
    /// </summary>
    public bool EnableAutoMigrate { get; set; } = true;

    /// <summary>
    /// 数据库命令超时时间（秒）
    /// </summary>
    public int SQLCommandTimeout { get; set; } = 30;

    /// <summary>
    /// 是否启懒加载
    /// </summary>
    public bool UseLazyLoading { get; set; } = false;

    /// <summary>
    /// 是否开启数据追踪
    /// </summary>
    public bool UseDataTracking { get; set; } = true;

    public bool EnableSensitiveDataLogging { get; set; } = false;

    public bool EnableShardingTable { get; set; } = true;

    public IList<string> ReadConnectionRefs { get; set; } = new List<string>();

    /// <summary>
    /// 从资源字典解析并缓存主库与读库连接串，资源缺失时抛出 <see cref="GirvsException"/>。
    /// </summary>
    public void ResolveConnectionStrings(
        IReadOnlyDictionary<string, GirvsInfrastructureResource> resources
    )
    {
        ArgumentNullException.ThrowIfNull(resources);

        var master = BuildConnectionString(GetResource(resources, ConnectionRef));
        var reads = ReadConnectionRefs
            .Select(reference => BuildConnectionString(GetResource(resources, reference)))
            .ToList();

        _connections = new ResolvedConnections(master, reads);
    }

    /// <summary>
    /// 获取主库连接串。
    /// </summary>
    public string GetMasterDataConnectionString() => GetConnections().Master;

    /// <summary>
    /// 随机获取一个读库连接串；未配置读库时返回主库连接串。
    /// </summary>
    public string GetSecureRandomReadDataConnectionString()
    {
        var connections = GetConnections();

        return connections.Reads.Count switch
        {
            0 => connections.Master,
            1 => connections.Reads[0],
            var count => connections.Reads[SecureRandomNumberGenerator.GetInt32(0, count)],
        };
    }

    private ResolvedConnections GetConnections()
    {
        if (_connections == null)
            ResolveConnectionStrings(Singleton<AppSettings>.Instance.Resources);

        return _connections;
    }

    private static GirvsInfrastructureResource GetResource(
        IReadOnlyDictionary<string, GirvsInfrastructureResource> resources,
        string name
    ) =>
        resources.TryGetValue(name ?? string.Empty, out var resource)
            ? resource
            : throw new GirvsException($"Resources:{name} 未配置");

    /// <summary>
    /// 按资源类型构建连接串，Settings 键约定与 Aspire 编排端一致。
    /// </summary>
    public string BuildConnectionString(GirvsInfrastructureResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return resource.Type?.ToLowerInvariant() switch
        {
            "mysql" => BuildMySqlConnectionString(resource),
            "sqlserver" => BuildSqlServerConnectionString(resource),
            _ => throw new GirvsException(
                $"Resources:{ConnectionRef}:Type '{resource.Type}' 不支持 EntityFrameworkCore"
            ),
        };
    }

    private string BuildMySqlConnectionString(GirvsInfrastructureResource resource)
    {
        var builder = new DbConnectionStringBuilder
        {
            ["Server"] = GetRequiredSetting(resource, "Host"),
            ["Database"] = GetDatabaseName(resource),
        };

        if (int.TryParse(resource.Settings.GetValueOrDefault("Port"), out var port))
            builder["Port"] = port;

        AddCredentials(builder, resource);
        builder["Allow User Variables"] = true;

        return builder.ConnectionString;
    }

    private string BuildSqlServerConnectionString(GirvsInfrastructureResource resource)
    {
        var host = GetRequiredSetting(resource, "Host");

        // SqlClient 不识别 Port 键，端口需并入 Data Source。
        if (int.TryParse(resource.Settings.GetValueOrDefault("Port"), out var port))
            host = $"{host},{port}";

        var builder = new DbConnectionStringBuilder
        {
            ["Data Source"] = host,
            ["Initial Catalog"] = GetDatabaseName(resource),
        };

        AddCredentials(builder, resource);

        // Aspire 编排的 SQL Server 使用自签名证书，默认信任；可通过 Settings 显式关闭。
        var trustServerCertificate =
            !resource.Settings.TryGetValue("TrustServerCertificate", out var trust)
            || !bool.TryParse(trust, out var configuredTrust)
            || configuredTrust;
        builder["TrustServerCertificate"] = trustServerCertificate;

        return builder.ConnectionString;
    }

    /// <summary>
    /// 资源未指定数据库名时回退到配置的 <see cref="Name"/>。
    /// </summary>
    private string GetDatabaseName(GirvsInfrastructureResource resource) =>
        resource.Settings.TryGetValue("Database", out var database)
        && !string.IsNullOrWhiteSpace(database)
            ? database
            : Name;

    private string GetRequiredSetting(GirvsInfrastructureResource resource, string key) =>
        resource.Settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new GirvsException($"Resources:{ConnectionRef}:Settings:{key} 未配置");

    private static void AddCredentials(
        DbConnectionStringBuilder builder,
        GirvsInfrastructureResource resource
    )
    {
        if (
            resource.Settings.TryGetValue("UserName", out var userName)
            && !string.IsNullOrWhiteSpace(userName)
        )
            builder["User ID"] = userName;

        if (
            resource.Settings.TryGetValue("Password", out var password)
            && !string.IsNullOrWhiteSpace(password)
        )
            builder["Password"] = password;
    }
}
