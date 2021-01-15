using System.Collections.Generic;

namespace Girvs.Domain.Configuration
{
    /// <summary>
    /// 表示启动CanteenPurchaseConfig配置参数
    /// </summary>
    public partial class GirvsConfig
    {
        public GirvsConfig()
        {
            Tasks = new List<TaskConfig>();
        }

        /// <summary>
        /// 获取或设置一个值，该值指示是否在生产环境中显示完整错误。它在开发环境中被忽略（始终启用）
        /// </summary>
        public bool DisplayFullErrorStack { get; set; }

        /// <summary>
        /// 默认缓存时间
        /// </summary>
        public int CacheTime { get; set; }

        /// <summary>
        /// 获取或设置一个值，指示是否应使用Redis服务器
        /// </summary>
        public bool RedisEnabled { get; set; }

        /// <summary>
        /// 获取或设置Redis连接字符串。启用Redis时使用
        /// </summary>
        public string RedisConnectionString { get; set; }

        /// <summary>
        /// 获取或设置特定的redis数据库;如果您需要使用特定的redis数据库，只需在此处设置其编号即可。如果应为每种数据类型使用不同的数据库，则设置NULL（默认使用）
        /// </summary>
        public int? RedisDatabaseId { get; set; }

        /// <summary>
        /// 获取或设置一个值，该值指示是否应将数据保护系统配置为在Redis数据库中保留密钥
        /// </summary>
        public bool UseRedisToStoreDataProtectionKeys { get; set; }

        /// <summary>
        /// 获取或设置一个值，指示是否应该使用Redis服务器进行缓存（而不是默认的内存缓存）
        /// </summary>
        public bool UseRedisForCaching { get; set; }

        /// <summary>
        /// 获取或设置一个值，指示是否应使用Redis服务器存储插件信息（而不是默认的plugin.json文件）
        /// </summary>
        public bool UseRedisToStorePluginsInfo { get; set; }

        /// <summary>
        /// 获取或设置一个值，该值指示商店所有者是否可以在安装期间安装样本数据
        /// </summary>
        public bool DisableSampleDataDuringInstallation { get; set; }

        /// <summary>
        /// 获取或设置一个值，该值指示是否将程序集加载到load-from上下文中，从而绕过某些安全检查。
        /// </summary>
        public bool UseUnsafeLoadAssembly { get; set; }

        /// <summary>
        /// 获取或设置一个值，该值指示是否将TempData存储在会话状态中。
        ///默认情况下，基于cookie的TempData提供程序用于在Cookie中存储TempData。
        /// </summary>
        public bool UseSessionStateTempDataProvider { get; set; }

        public bool TenantEnabled { get; set; }

        public bool WhetherTheTenantIsInvolvedInManagement { get; set; }
        public List<FunctionalModuleConfig> FunctionalModules { get; set; }
        public List<TaskConfig> Tasks { get; set; }
        public ClaimValueConfig ClaimValueConfig { get; set; } = new ClaimValueConfig();
        public IList<DataConnectionConfig> DataConnectionConfigs { get; set; }
    }
    


    public class DataConnectionConfig
    {
        /// <summary>
        /// 数据库名称
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// 数据库类型
        /// </summary>
        public UseDataType UseDataType { get; set; }
        
        /// <summary>
        /// 数据库版本号
        /// </summary>
        public string VersionNumber { get; set; }

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
        
        /// <summary>
        /// 主数据库连接字符串
        /// </summary>
        public string MasterDataConnectionString { get; set; }
        
        /// <summary>
        /// 存数据库字符串集,可以是多个
        /// </summary>
        public IList<string> ReadDataConnectionString { get; set; }
    }
}