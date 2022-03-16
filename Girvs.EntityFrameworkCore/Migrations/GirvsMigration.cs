using Girvs.BusinessBasis.Entities;
using Girvs.EntityFrameworkCore.DbContextExtensions;
using Girvs.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Girvs.EntityFrameworkCore.Migrations
{
    public abstract class GirvsMigration : Migration
    {
        public virtual string GetShardingTableName<T>() where T : Entity
        {
            return EngineContext.Current.GetMigrationEntityTableName<T>();
        }

        public virtual bool IsCreateShardingTable<T>() where T :Entity
        {
            return EngineContext.Current.IsNeedShardingTable<T>();
        }

        public virtual string GetShardingForeignKey<T>(string OriginalKeyName) where T : Entity
        {
            var suffix = EngineContext.Current.GetSafeShardingTableSuffix();
            return $"{OriginalKeyName}{suffix}";
        }
    }
}