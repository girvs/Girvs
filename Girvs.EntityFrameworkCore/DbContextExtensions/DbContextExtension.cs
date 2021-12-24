using Girvs.EntityFrameworkCore.Configuration;
using Girvs.EntityFrameworkCore.Enumerations;
using Girvs.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Girvs.EntityFrameworkCore.DbContextExtensions
{
    public static class DbContextExtension
    {
        public static void SwitchReadWriteDataBase(this DbContext dbContext, DataBaseWriteAndRead dataBaseWriteAndRead)
        {
            var currentDbContextConfig = EngineContext.Current.GetAppModuleConfig<DbConfig>()
                ?.GetDataConnectionConfig(dbContext.GetType());

            var logger = EngineContext.Current.Resolve<ILogger<object>>();
            var connStr = dataBaseWriteAndRead == DataBaseWriteAndRead.Write
                ? currentDbContextConfig?.MasterDataConnectionString
                : currentDbContextConfig?.GetSecureRandomReadDataConnectionString();

            var conn = dbContext.Database.GetDbConnection();

            logger?.LogInformation(
                $"当前DbContextId为：{dbContext.ContextId.InstanceId.ToString()}，当前数据的状态为：{conn.State}，切换数据库模式为：{dataBaseWriteAndRead}，数据库字符串为：{connStr}");

            conn.ConnectionString = connStr;
        }
    }
}