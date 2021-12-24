using System.Collections.Generic;
using System.Data.Common;
using Girvs.EntityFrameworkCore.TableExists.Abstractions;
using Microsoft.EntityFrameworkCore;
using ShardingCore.Sharding.Abstractions;

namespace Girvs.EntityFrameworkCore.TableExists
{
    public  class SqlServerTableEnsureManager<TShardingDbContext> : AbstractTableEnsureManager<TShardingDbContext> where TShardingDbContext : DbContext, IShardingDbContext
    {
        private const string Tables = "Tables";
        private const string TABLE_NAME = "TABLE_NAME";

        public override ISet<string> DoGetExistTables(DbConnection connection, string dataSourceName)
        {
            ISet<string> result = new HashSet<string>();
            using (var dataTable = connection.GetSchema(Tables))
            {
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    result.Add(dataTable.Rows[i][TABLE_NAME].ToString());
                }
            }
            return result;
        }
    }
}
