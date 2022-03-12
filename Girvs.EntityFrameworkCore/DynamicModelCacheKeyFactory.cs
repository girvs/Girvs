using Girvs.EntityFrameworkCore.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Girvs.EntityFrameworkCore
{
    public class DynamicModelCacheKeyFactory : IModelCacheKeyFactory
    {
        public object Create(DbContext context)
        {
            return context is GirvsDbContext shardingContext
                ? (context.GetType(), shardingContext.ShardingSuffix)
                : (object) context.GetType();
        }
    }
}