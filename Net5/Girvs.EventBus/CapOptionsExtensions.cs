using DotNetCore.CAP;

namespace Girvs.EventBus
{
    public static class CapOptionsExtensions
    {
        public static void UseGrivsConfigDataBase(this CapOptions options)
        {
            // var dbConfig =
            //     EngineContext.Current.Resolve<AppSettings>()[nameof(DbConfig)] as DbConfig;
            //
            // var typeFinder = new WebAppTypeFinder();
            // var dbContexts = typeFinder.FindOfType(typeof
            //     (IDbContext)).Where(x => !x.IsAbstract && !x.IsInterface).ToList();
            // if (!dbContexts.Any()) return;
            //
            // var dbContext = dbContexts.Select(dbContextType =>
            //     EngineContext.Current.Resolve(dbContextType) as GirvsDbContext).FirstOrDefault();
            //
            // if (dbContext == null)
            //     return;
            //
            // var config = dbConfig.DataConnectionConfigs.FirstOrDefault(x => x.Name == dbContext.DbConfigName);
            // if (config == null)
            //     return;
            //
            // switch (config.UseDataType)
            // {
            //     case UseDataType.MsSql:
            //         options.UseSqlServer(config.MasterDataConnectionString);
            //         break;
            //     case UseDataType.MySql:
            //         options.UseMySql(config.MasterDataConnectionString);
            //         break;
            //     case UseDataType.SqlLite:
            //         options.UseSqlite(config.MasterDataConnectionString);
            //         break;
            //     case UseDataType.Oracle:
            //         options.UseOracle(config.MasterDataConnectionString);
            //         break;
            // }
        }
    }
}