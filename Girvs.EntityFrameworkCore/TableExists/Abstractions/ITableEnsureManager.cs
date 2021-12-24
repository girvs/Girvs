using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using ShardingCore.Sharding.Abstractions;

namespace Girvs.EntityFrameworkCore.TableExists.Abstractions
{
    public interface ITableEnsureManager
    {
        ISet<string> GetExistTables(string dataSourceName);
        ISet<string> GetExistTables(IShardingDbContext shardingDbContext, string dataSourceName);
    }
    public interface ITableEnsureManager<TShardingDbContext>: ITableEnsureManager where TShardingDbContext : DbContext, IShardingDbContext
    {
    }
}
