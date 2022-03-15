using System.Collections.Generic;
using Girvs.EntityFrameworkCore.Context;
using Girvs.Extensions;
using Girvs.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Girvs.EntityFrameworkCore.MigrationShardingTable
{
    public static class MigrationShardingTable
    {
        private static readonly IList<string> TenantIds = new List<string>();

        private static object _async = new object();

        public static string GetCurrentTenantId()
        {
            try
            {
                var currentTenantId = EngineContext.Current.ClaimManager.GetTenantId();
                return currentTenantId.Replace("-", "");
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 创建租户用户表
        /// </summary>
        /// <param name="dbContext"></param>
        public static void CreateTenantShardTable(DbContext dbContext)
        {
            lock (_async)
            {
                var currentTenantId = GetCurrentTenantId();
                if (currentTenantId.IsNullOrEmpty() || TenantIds.Contains(currentTenantId))
                    return;
                // dbContext.CreateTenantShardTable();
                dbContext.Database.Migrate();
            }
        }
    }
}