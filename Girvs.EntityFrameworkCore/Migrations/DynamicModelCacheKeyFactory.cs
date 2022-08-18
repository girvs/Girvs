namespace Girvs.EntityFrameworkCore.Migrations;

public class DynamicModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context) => Create(context, false);

    public object Create(DbContext context, bool designTime)
    {
        return context is GirvsDbContext
            ? (context.GetType(), EngineContext.Current.GetSafeShardingTableSuffix(), designTime)
            : (object) context.GetType();
    }
}