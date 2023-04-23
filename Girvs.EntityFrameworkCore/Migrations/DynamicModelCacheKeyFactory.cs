namespace Girvs.EntityFrameworkCore.Migrations;

public class DynamicModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context) => Create(context, false);

    public object Create(DbContext context, bool designTime)
    {
        var related = EngineContext.Current.GetShardingTableRelatedByDbContext(context.GetType());
        
        return context is GirvsDbContext
            ? (context.GetType(), related.GetCurrentMigrationsHistoryShardingTableSuffix(), designTime)
            : (object) context.GetType();
    }
}