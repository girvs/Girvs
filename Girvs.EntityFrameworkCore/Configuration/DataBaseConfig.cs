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

    /// <summary>
    /// 引用 Resources 中的数据库资源：索引 0 为资源键格式串，索引 1 起为 string.Format 参数。
    /// 同一组参数既用于组合资源键，也用于格式化资源的 Settings:ConnectionString。
    /// </summary>
    public string[] ConnectionRef { get; set; }

    /// <summary>
    /// 连接配置名称，用于按 DbContext 匹配。
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

    /// <summary>引用 Resources 中的读库资源；每项独立带格式参数。</summary>
    public IList<string[]> ReadConnectionRefs { get; set; } = new List<string[]>();

    /// <summary>
    /// 从资源字典解析并缓存主库与读库连接串，资源缺失时抛出 <see cref="GirvsException"/>。
    /// </summary>
    public void ResolveConnectionStrings(
        IReadOnlyDictionary<string, GirvsInfrastructureResource> resources
    )
    {
        ArgumentNullException.ThrowIfNull(resources);

        var master = ResolveConnectionString(resources, ConnectionRef);
        var reads = ReadConnectionRefs
            .Select(reference => ResolveConnectionString(resources, reference))
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

    /// <summary>
    /// 按资源配置的 Settings:ConnectionString 模板构建连接串，并用当前 ConnectionRef 的参数格式化。
    /// </summary>
    public string BuildConnectionString(GirvsInfrastructureResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        var resourceKey = GetDisplayResourceKey();
        ValidateType(resourceKey, resource);
        return FormatConnectionString(resourceKey, resource, ConnectionRef ?? []);
    }

    private static string ResolveConnectionString(
        IReadOnlyDictionary<string, GirvsInfrastructureResource> resources,
        string[] reference
    )
    {
        var resourceKey = InfrastructureResourceReferenceResolver.FormatResourceKey(reference);

        var resource = resources.TryGetValue(resourceKey, out var value)
            ? value
            : throw new GirvsException($"Resources:{resourceKey} 未配置");

        ValidateType(resourceKey, resource);
        return FormatConnectionString(resourceKey, resource, reference);
    }

    private static string FormatConnectionString(
        string resourceKey,
        GirvsInfrastructureResource resource,
        string[] reference
    )
    {
        var template = resource.Settings?.GetValueOrDefault("ConnectionString");
        if (string.IsNullOrWhiteSpace(template))
            throw new GirvsException($"Resources:{resourceKey}:Settings:ConnectionString 未配置");

        return InfrastructureResourceReferenceResolver.FormatValue(template, reference);
    }

    private static void ValidateType(string resourceKey, GirvsInfrastructureResource resource)
    {
        if (
            string.Equals(resource.Type, "mysql", StringComparison.OrdinalIgnoreCase)
            || string.Equals(resource.Type, "sqlserver", StringComparison.OrdinalIgnoreCase)
        )
            return;

        throw new GirvsException(
            $"Resources:{resourceKey}:Type '{resource.Type}' 不支持 EntityFrameworkCore"
        );
    }

    private string GetDisplayResourceKey()
    {
        return InfrastructureResourceReferenceResolver.IsConfigured(ConnectionRef)
            ? InfrastructureResourceReferenceResolver.FormatResourceKey(ConnectionRef)
            : "<未配置>";
    }
}
