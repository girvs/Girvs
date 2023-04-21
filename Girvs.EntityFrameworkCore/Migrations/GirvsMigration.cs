namespace Girvs.EntityFrameworkCore.Migrations;

public abstract class GirvsMigration : Migration
{
    public virtual string GetShardingTableName<T>() where T : Entity
    {
        return EngineContext.Current.GetMigrationEntityTableName<T>();
    }

    public virtual bool IsCreateShardingTable<T>() where T : Entity
    {
        return EngineContext.Current.IsNeedShardingTable<T>();
    }

    public virtual string GetShardingForeignKey<T>(string OriginalKeyName) where T : Entity
    {
        var suffix = EngineContext.Current.GetSafeShardingTableSuffix<T>();
        var result = $"{OriginalKeyName}{suffix}";
        if (result.Length > 64)
        {
            return $"PK_{result.ToMd5()}";
        }

        return result;
    }
}