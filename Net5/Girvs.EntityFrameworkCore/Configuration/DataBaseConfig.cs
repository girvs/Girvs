using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Girvs.Configuration;

namespace Girvs.EntityFrameworkCore.Configuration
{
    public enum UseDataType
    {
        [EnumMember(Value = "mssql")]
        MsSql,
        [EnumMember(Value = "mysql")]
        MySql,
        [EnumMember(Value = "sqllite")]
        SqlLite,
        [EnumMember(Value = "oracle")]
        Oracle
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
    }

    public class DataConnectionConfig
    {
        /// <summary>
        /// 数据库名称
        /// </summary>
        public string Name { get; set; } = "default";

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

        /// <summary>
        /// 主数据库连接字符串
        /// </summary>
        public string MasterDataConnectionString { get; set; } =
            "Server=192.168.51.166;database=Wb_BasicManagement;User ID=root;Password=123456;Character Set=utf8;";

        /// <summary>
        /// 从数据库字符串集,可以是多个
        /// </summary>
        public IList<string> ReadDataConnectionString { get; set; } = new List<string>();
    }
}