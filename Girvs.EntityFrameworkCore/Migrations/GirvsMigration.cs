namespace Girvs.EntityFrameworkCore.Migrations;

public abstract class GirvsMigration : Migration
{
    public virtual string GetShardingTableName<T>() where T : Entity
    {
        var entityShardingSet = EngineContext.Current.GetEntityShardingTableParameter<T>();
        return entityShardingSet.GetCurrentShardingTableName();
    }

    public virtual bool IsCreateShardingTable<T>() where T : Entity
    {
        //如果是进行首次迁移则，而直接还原所有的表
        if (EngineContext.Current.GetTenantShardingTableSuffix().IsNullOrEmpty())
            return true;

        var entityShardingSet = EngineContext.Current.GetEntityShardingTableParameter<T>();
        return entityShardingSet.IsNeedShardingTable;
    }

    public virtual string GetShardingForeignKey<T>(string OriginalKeyName) where T : Entity
    {
        var entityShardingSet = EngineContext.Current.GetEntityShardingTableParameter<T>();
        var suffix = entityShardingSet.GetCurrentShrdingTableSuffix();
        var result = $"{OriginalKeyName}{suffix}";
        if (result.Length > 64)
        {
            return $"PK_{result.ToMd5()}";
        }

        return result;
    }

    public virtual string GetOldShardingForeignKey<T>(string OriginalKeyName) where T : Entity
    {
        var entityShardingSet = EngineContext.Current.GetEntityShardingTableParameter<T>();
        var suffix = entityShardingSet.GetCurrentShrdingTableSuffix();
        return $"{OriginalKeyName}{suffix}";
    }
}