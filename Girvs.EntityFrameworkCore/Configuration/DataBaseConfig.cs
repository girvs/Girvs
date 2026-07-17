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
}

public class DataConnectionConfig
{
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

    public string BuildConnectionString(Resource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        if (!string.Equals(resource.Type, "mysql", StringComparison.OrdinalIgnoreCase))
            throw new GirvsException($"Resources:{ConnectionRef}:Type 必须为 mysql");

        if (!resource.Settings.TryGetValue("Host", out var host) || string.IsNullOrWhiteSpace(host))
            throw new GirvsException($"Resources:{ConnectionRef}:Settings:Host 未配置");

        var builder = new System.Data.Common.DbConnectionStringBuilder { ["Server"] = host };

        if (
            resource.Settings.TryGetValue("Database", out var database)
            && !string.IsNullOrWhiteSpace(database)
        )
            builder["Database"] = database;

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

        return builder.ConnectionString;
    }
}
