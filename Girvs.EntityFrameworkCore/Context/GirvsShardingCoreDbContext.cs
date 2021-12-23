using Girvs.EntityFrameworkCore.Enumerations;
using Microsoft.EntityFrameworkCore;
using ShardingCore.Core.VirtualRoutes.TableRoutes.RouteTails.Abstractions;
using ShardingCore.Sharding;
using ShardingCore.Sharding.Abstractions;

namespace Girvs.EntityFrameworkCore.Context
{
    public abstract class GirvsShardingCoreDbContext : AbstractShardingDbContext, IShardingTableDbContext, IDbContext
    {
        public GirvsShardingCoreDbContext(DbContextOptions options) : base(options)
        {

        }

        public IRouteTail RouteTail { get; set; }

        public virtual void SwitchReadWriteDataBase(DataBaseWriteAndRead dataBaseWriteAndRead)
        {

        }
    }
}