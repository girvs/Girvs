using System.Collections.Generic;
using Girvs.EntityFrameworkCore.TableExists.Abstractions;
using Microsoft.EntityFrameworkCore;
using ShardingCore.Sharding.Abstractions;

namespace Girvs.EntityFrameworkCore.TableExists
{
    public class EmptyTableEnsureManager<TShardingDbContext> : ITableEnsureManager<TShardingDbContext> where TShardingDbContext : DbContext, IShardingDbContext
    {
        public ISet<string> GetExistTables(string dataSourceName)
        {
            return new HashSet<string>();
        }

        public ISet<string> GetExistTables(IShardingDbContext shardingDbContext, string dataSourceName)
        {
            return new HashSet<string>();
        }
    }
}
