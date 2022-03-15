using Girvs.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Girvs.EntityFrameworkCore.Context
{
    public abstract class GirvsDbContext : DbContext
    {
        public GirvsDbContext(DbContextOptions options) : base(options)
        {
        }

        public virtual string GetRelationalTableName<TEntity>()
        {
            var entityType = Model.FindEntityType(typeof(TEntity));
            return entityType.GetTableName();
        }
    }
}