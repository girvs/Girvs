using System;
using System.Linq;
using Girvs.BusinessBasis.Entities;
using Girvs.EntityFrameworkCore.Configuration;
using Girvs.EntityFrameworkCore.Enumerations;
using Girvs.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Girvs.EntityFrameworkCore.DbContextExtensions
{
    public static class DbContextExtension
    {
        public static void SwitchReadWriteDataBase(this DbContext dbContext, DataBaseWriteAndRead dataBaseWriteAndRead)
        {
            var currentDbContextConfig = EngineContext.Current.GetAppModuleConfig<DbConfig>()
                ?.GetDataConnectionConfig(dbContext.GetType());

            var logger = EngineContext.Current.Resolve<ILogger<object>>();
            var connStr = dataBaseWriteAndRead == DataBaseWriteAndRead.Write
                ? currentDbContextConfig?.MasterDataConnectionString
                : currentDbContextConfig?.GetSecureRandomReadDataConnectionString();

            var conn = dbContext.Database.GetDbConnection();

            logger?.LogInformation(
                $"当前DbContextId为：{dbContext.ContextId.InstanceId.ToString()}，当前数据的状态为：{conn.State}，切换数据库模式为：{dataBaseWriteAndRead}，数据库字符串为：{connStr}");

            conn.ConnectionString = connStr;
        }


        /// <summary>
        /// 移除不需分表的实体
        /// </summary>
        /// <param name="dbContext"></param>
        public static void RemoveDbContextRelationModelNotShardTableEntity(this DbContext dbContext)
        {
            var contextModel = dbContext.Model as Model;
            var contextModelRelationalModel = contextModel.RelationalModel as RelationalModel;

            foreach (var table in contextModelRelationalModel.Tables)
            {
                var isRemove = table.Value.EntityTypeMappings.Any(x =>
                {
                    var entityType = x.EntityType.ClrType;
                    return entityType.IsAssignableTo(typeof(ITenantShardingTable)) &&
                           entityType.GetProperties().Any(x => x.Name == nameof(IIncludeMultiTenant<object>.TenantId));
                });

                if (!isRemove)
                {
                    contextModelRelationalModel.Tables.Remove(table.Key);
                }
            }
        }


        /// <summary>
        /// 移除所有的除了我指定的那个类型
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="shardingType"></param>
        public static void RemoveDbContextRelationModelTableEntityWithType(this DbContext dbContext,
            Type shardingType)
        {
            var contextModel = dbContext.Model as Model;
            var contextModelRelationalModel = contextModel.RelationalModel as RelationalModel;
            var valueTuples = contextModelRelationalModel.Tables
                .Where(o => o.Value.EntityTypeMappings.All(m => m.EntityType.ClrType != shardingType))
                .Select(o => o.Key).ToList();
            for (int i = 0; i < valueTuples.Count; i++)
            {
                contextModelRelationalModel.Tables.Remove(valueTuples[i]);
            }
        }

        /// <summary>
        /// 移除所有的除了我指定的那个类型
        /// </summary>
        public static void RemoveDbContextRelationModelTableEntityWithType<T>(this DbContext dbContext)
            where T : Entity
        {
            RemoveDbContextRelationModelTableEntityWithType(dbContext, typeof(T));
        }

        /// <summary>
        /// 创建表
        /// </summary>
        /// <param name="shardingEntityType"></param>
        public static void CreateTable(this DbContext dbContext, Type shardingEntityType)
        {
            // 需要判断表是否已经存在

            dbContext.RemoveDbContextRelationModelTableEntityWithType(shardingEntityType);
            var databaseCreator = dbContext.Database.GetService<IDatabaseCreator>() as RelationalDatabaseCreator;
            try
            {
                databaseCreator.CreateTables();
            }
            catch (Exception ex)
            {
                // throw new GirvsException($" create table error :{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 创建租户表
        /// </summary>
        /// <param name="dbContext"></param>
        /// <exception cref="GirvsException"></exception>
        public static void CreateTenantShardTable(this DbContext dbContext)
        {
            // 需要判断表是否已经存在

            dbContext.RemoveDbContextRelationModelNotShardTableEntity();
            var databaseCreator = dbContext.Database.GetService<IDatabaseCreator>() as RelationalDatabaseCreator;
            try
            {
                databaseCreator.CreateTables();
            }
            catch (Exception ex)
            {
                // throw new GirvsException($" create table error :{ex.Message}", ex);
            }
        }
    }
}