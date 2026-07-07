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
            throw new ArgumentNullException($"{nameof(activeProvider)} argument is null");

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
