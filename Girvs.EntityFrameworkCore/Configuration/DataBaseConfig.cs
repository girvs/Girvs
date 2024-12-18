namespace Girvs.EntityFrameworkCore.Configuration;

public enum UseDataType
{
    [EnumMember(Value = "mssql")]
    MsSql,

    [EnumMember(Value = "mysql")]
    MySql,

#if NET8_0
    [EnumMember(Value = "sqllite")]
    SqlLite,

    [EnumMember(Value = "oracle")]
    Oracle
#endif
}

public class DbConfig : IAppModuleConfig
{
    public DbConfig()
    {
        DataConnectionConfigs = new List<DataConnectionConfig>();
    }

    public ICollection<DataConnectionConfig> DataConnectionConfigs { get; set; }

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
    /// <summary>
    /// 数据库名称
    /// </summary>
    public string Name { get; set; } = "default";

    /// <summary>
    /// 启用自动还原数据库
    /// </summary>
    public bool EnableAutoMigrate { get; set; } = true;

    /// <summary>
    /// 数据库类型
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UseDataType UseDataType { get; set; } = UseDataType.MsSql;

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

    /// <summary>
    /// 主数据库连接字符串
    /// </summary>
    public string MasterDataConnectionString { get; set; } =
        "Server=192.168.51.166;database=Wb_BasicManagement;User ID=root;Password=123456;Character Set=utf8;";

    /// <summary>
    /// 从数据库字符串集,可以是多个
    /// </summary>
    public IList<string> ReadDataConnectionString { get; set; } = new List<string>();

    // public DbHostServerPort MasterDatabaseHost { get; set; } = new DbHostServerPort();
    // public IList<DbHostServerPort> SlaveDatabaseHost { get; set; } = new List<DbHostServerPort>();

    public string GetSecureRandomReadDataConnectionString()
    {
        if (ReadDataConnectionString == null || !ReadDataConnectionString.Any())
        {
            return MasterDataConnectionString;
        }
        else
        {
            if (ReadDataConnectionString.Count == 1)
            {
                return ReadDataConnectionString[0];
            }
            else
            {
                var index = SecureRandomNumberGenerator.GetInt32(0, ReadDataConnectionString.Count);
                return ReadDataConnectionString[index];
            }
        }
    }
}

public class DbHostServerPort
{
    public string Server { get; set; } = "192.168.51.166";
    public int Port { get; set; } = 3306;
    public string DatabaseName { get; set; } = "Wb_BasicManagement";
    public string UserId { get; set; } = "root";
    public string Password { get; set; } = "123456";
}
