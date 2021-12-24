using Girvs.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Girvs.EntityFrameworkCore
{
    public class DynamicModelCacheKeyFactory : IModelCacheKeyFactory
    {
        public object Create(DbContext context)
        {
            var tenantId = EngineContext.Current.ClaimManager.GetTenantId();
            return (context.GetType(), tenantId);
        }
    }
}