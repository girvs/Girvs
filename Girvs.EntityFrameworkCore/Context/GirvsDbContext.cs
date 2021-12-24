using Microsoft.EntityFrameworkCore;

namespace Girvs.EntityFrameworkCore.Context
{
    public abstract class GirvsDbContext : GirvsShardingCoreDbContext
    {
        public GirvsDbContext(DbContextOptions options) : base(options)
        {
        }
    }
}