namespace Girvs.EntityFrameworkCore.Migrations;

public class DynamicModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context) => Create(context, false);
    
    public object Create(DbContext context, bool designTime)
    {
        var tenantId = EngineContext.Current.ClaimManager.IdentityClaim.TenantId;
        return context is GirvsDbContext
            ? (context.GetType(), tenantId)
            : (object) context.GetType();
    }
}