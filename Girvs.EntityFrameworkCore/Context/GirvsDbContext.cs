using System.Linq;
using Girvs.EntityFrameworkCore.DbContextExtensions;
using Girvs.EntityFrameworkCore.Enumerations;
using Girvs.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Girvs.EntityFrameworkCore.Context
{
    public abstract class GirvsDbContext : GirvsShardingCoreDbContext
    {
        private readonly ILogger<DbContext> _logger;

        public GirvsDbContext(DbContextOptions options) : base(options)
        {
            _logger = EngineContext.Current.Resolve<ILogger<DbContext>>();
        }

        public override void SwitchReadWriteDataBase(DataBaseWriteAndRead dataBaseWriteAndRead)
        {
            var connStr = GetDbConnectionString(dataBaseWriteAndRead);
            var conn = Database.GetDbConnection();
            _logger?.LogInformation(
                $"当前DbContextId为：{ContextId.InstanceId.ToString()}，当前数据的状态为：{conn.State}，切换数据库模式为：{dataBaseWriteAndRead}，数据库字符串为：{connStr}");
            conn.ConnectionString = connStr;
        }

        private string GetDbConnectionString(DataBaseWriteAndRead dataBaseWriteAndRead)
        {
            var dataConnectionConfig = DataProviderServiceExtensions.GetDataConnectionConfig(GetType());

            if (dataBaseWriteAndRead == DataBaseWriteAndRead.Write ||
                !dataConnectionConfig.ReadDataConnectionString.Any())
            {
                return dataConnectionConfig.MasterDataConnectionString;
            }
            else
            {
                if (dataConnectionConfig.ReadDataConnectionString.Count == 1)
                {
                    return dataConnectionConfig.ReadDataConnectionString[0];
                }
                else
                {
                    var index = SecureRandomNumberGenerator.GetInt32(0,
                        dataConnectionConfig.ReadDataConnectionString.Count);
                    return dataConnectionConfig.ReadDataConnectionString[index];
                }
            }
        }
    }
}