using Girvs.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Girvs.EntityFrameworkCore.Context
{
    public abstract class GirvsDbContext : DbContext
    {
        public GirvsDbContext(DbContextOptions options) : base(options)
        {
            ShardingSuffix = EngineContext.Current.ClaimManager.GetTenantId();
        }

        public string ShardingSuffix { get; protected set; }
    }
}