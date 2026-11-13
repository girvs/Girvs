// 分表租户需要在运行时替换迁移程序集。EF Core 11 仍未提供等价的公共 API，
// 只能继续依赖 MigrationsAssembly；升级 EF Core 主版本时必须回归验证该构造函数签名。
#pragma warning disable EF1001

namespace Girvs.EntityFrameworkCore.Migrations;

public class GirvsMigrationByTenantAssembly(
    ICurrentDbContext currentContext,
    IDbContextOptions options,
    IMigrationsIdGenerator idGenerator,
    IDiagnosticsLogger<DbLoggerCategory.Migrations> logger
) : MigrationsAssembly(currentContext, options, idGenerator, logger)
{
    public override Migration CreateMigration(TypeInfo migrationClass, string activeProvider)
    {
        if (activeProvider == null)
            throw new ArgumentNullException(nameof(activeProvider));

        var hasCtorWithSchema = migrationClass.GetConstructor([typeof(string)]) != null;

        if (hasCtorWithSchema)
        {
            var entityShardingSet = EngineContext.Current.GetEntityShardingTableParameter(
                migrationClass.AsType()
            );
            var migration = (Migration)
                Activator.CreateInstance(
                    migrationClass.AsType(),
                    entityShardingSet.GetCurrentShrdingTableSuffix()
                );
            migration.ActiveProvider = activeProvider;
            return migration;
        }

        return base.CreateMigration(migrationClass, activeProvider);
    }
}

#pragma warning restore EF1001
