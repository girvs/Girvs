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

    /// <summary>引用 Resources 中的 MySQL 资源键。</summary>
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
    /// 数据库版本号
    /// </summary>
    public string VersionNumber { get; set; } = "2008";

    /// <summary>
    /// 数据库连接超时时间设置
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

    /// <summary>
    /// 启用行分页
    /// 获取或设置一个值，该值指示是否使用与SQL Server 2008和SQL Server 2008R2的向后兼容性
    /// </summary>
    public bool UseRowNumberForPaging { get; set; } = true;

    public bool EnableSensitiveDataLogging { get; set; } = false;

    public bool EnableShardingTable { get; set; } = true;

    public IList<string> ReadConnectionRefs { get; set; } = new List<string>();

    // public DbHostServerPort MasterDatabaseHost { get; set; } = new DbHostServerPort();
    // public IList<DbHostServerPort> SlaveDatabaseHost { get; set; } = new List<DbHostServerPort>();

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

    public string BuildConnectionString(GirvsInfrastructureResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        if (!string.Equals(resource.Type, "mysql", StringComparison.OrdinalIgnoreCase))
            throw new GirvsException($"Resources:{ConnectionRef}:Type 必须为 mysql");

        if (!resource.Settings.TryGetValue("Host", out var host) || string.IsNullOrWhiteSpace(host))
            throw new GirvsException($"Resources:{ConnectionRef}:Settings:Host 未配置");

        var builder = new System.Data.Common.DbConnectionStringBuilder { ["Server"] = host };

        //如果资源中未指定数据库名称，则使用name为数据库名称
        if (
            resource.Settings.TryGetValue("Database", out var database)
            && !string.IsNullOrWhiteSpace(database)
        )
        {
            builder["Database"] = database;
        }
        else
        {
            builder["Database"] = Name;
        }

        if (int.TryParse(resource.Settings.GetValueOrDefault("Port"), out var port))
            builder["Port"] = port;

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

        builder["Allow User Variables"] = true;

        return builder.ConnectionString;
    }
}
