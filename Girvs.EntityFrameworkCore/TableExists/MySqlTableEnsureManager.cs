﻿using System.Collections.Generic;
using System.Data.Common;
using Girvs.EntityFrameworkCore.TableExists.Abstractions;
using Microsoft.EntityFrameworkCore;
using ShardingCore.Sharding.Abstractions;

namespace Girvs.EntityFrameworkCore.TableExists
{
    public class MySqlTableEnsureManager<TShardingDbContext> : AbstractTableEnsureManager<TShardingDbContext> where TShardingDbContext : DbContext, IShardingDbContext
    {
        private const string Tables = "Tables";
        private const string TABLE_SCHEMA = "TABLE_SCHEMA";
        private const string TABLE_NAME = "TABLE_NAME";

        public override ISet<string> DoGetExistTables(DbConnection connection, string dataSourceName)
        {
            var database = connection.Database;
            ISet<string> result = new HashSet<string>();
            using (var dataTable = connection.GetSchema(Tables))
            {
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    if (dataTable.Rows[i][TABLE_SCHEMA].Equals(database))
                        result.Add(dataTable.Rows[i][TABLE_NAME].ToString());
                }
            }
            return result;
        }
    }
}
