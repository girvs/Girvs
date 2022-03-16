using Girvs.EntityFrameworkCore.DbContextExtensions;
using Girvs.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Girvs.EntityFrameworkCore.Migrations
{
    public class GirvsTenantModelCacheKeyFactory<TContext> : ModelCacheKeyFactory
        where TContext : DbContext
    {

        public override object Create(DbContext context)
        {
            var dbContext = context as TContext;
            return new TenantModelCacheKey<TContext>(dbContext, EngineContext.Current.GetSafeShardingTableSuffix());
        }

        public GirvsTenantModelCacheKeyFactory(ModelCacheKeyFactoryDependencies dependencies) : base(dependencies)
        {
        }
    }

    internal sealed class TenantModelCacheKey<TContext> : ModelCacheKey
        where TContext : DbContext
    {
        private readonly string _identifier;
        public TenantModelCacheKey(TContext context, string identifier) : base(context)
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
}