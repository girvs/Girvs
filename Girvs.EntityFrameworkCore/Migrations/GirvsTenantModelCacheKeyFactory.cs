namespace Girvs.EntityFrameworkCore.Migrations;

public class GirvsTenantModelCacheKeyFactory<TContext> : ModelCacheKeyFactory
    where TContext : DbContext
{
    public override object Create(DbContext context, bool designTime)
    {
        var dbContext = context as TContext;
        var related = EngineContext.Current.GetShardingTableRelatedByDbContext(context.GetType());
        return new TenantModelCacheKey<TContext>(dbContext, related.GetCurrentMigrationsHistoryShardingTableSuffix(),
            designTime);
    }

    public override object Create(DbContext context) => Create(context, false);

    public GirvsTenantModelCacheKeyFactory(ModelCacheKeyFactoryDependencies dependencies) : base(dependencies)
    {
    }
}

internal sealed class TenantModelCacheKey<TContext> : ModelCacheKey
    where TContext : DbContext
{
    private readonly string _identifier;

    public TenantModelCacheKey(TContext context, string identifier, bool designTime) : base(context, designTime)
    {
        _identifier = identifier;
    }

    protected override bool Equals(ModelCacheKey other)
    {
        return base.Equals(other) && (other as TenantModelCacheKey<TContext>)?._identifier == _identifier;
    }

    public override int GetHashCode()
    {
        var hashCode = base.GetHashCode();
        if (_identifier != null)
        {
            hashCode ^= _identifier.GetHashCode();
        }

        return hashCode;
    }
}