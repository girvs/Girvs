using System;
using System.Linq;
using Girvs.BusinessBasis.Entities;
using Girvs.EntityFrameworkCore.DbContextExtensions;
using Girvs.EntityFrameworkCore.Enumerations;
using Girvs.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Girvs.EntityFrameworkCore.Context
{
    public abstract class GirvsDbContext : GirvsShardingCoreDbContext
    {
        private readonly ILogger<DbContext> _logger;

        public GirvsDbContext(DbContextOptions options) : base(options)
        {
            _logger = EngineContext.Current.Resolve<ILogger<DbContext>>();
        }

        public override void SwitchReadWriteDataBase(DataBaseWriteAndRead dataBaseWriteAndRead)
        {
            var connStr = GetDbConnectionString(dataBaseWriteAndRead);
            var conn = Database.GetDbConnection();
            _logger?.LogInformation(
                $"当前DbContextId为：{ContextId.InstanceId.ToString()}，当前数据的状态为：{conn.State}，切换数据库模式为：{dataBaseWriteAndRead}，数据库字符串为：{connStr}");
            conn.ConnectionString = connStr;
        }

        private string GetDbConnectionString(DataBaseWriteAndRead dataBaseWriteAndRead)
        {
            var dataConnectionConfig = DataProviderServiceExtensions.GetDataConnectionConfig(GetType());

            if (dataBaseWriteAndRead == DataBaseWriteAndRead.Write || !dataConnectionConfig.ReadDataConnectionString.Any())
            {
                return dataConnectionConfig.MasterDataConnectionString;
            }
            else
            {
                if (dataConnectionConfig.ReadDataConnectionString.Count == 1)
                {
                    return dataConnectionConfig.ReadDataConnectionString[0];
                }
                else
                {
                    var index = SecureRandomNumberGenerator.GetInt32(0,
                        dataConnectionConfig.ReadDataConnectionString.Count);
                    return dataConnectionConfig.ReadDataConnectionString[index];
                }
            }
        }

        #region OnModel Girvs Default

        public static void OnModelCreatingBaseEntityAndTableKey<TEntity, TKey>(EntityTypeBuilder<TEntity> entity)
            where TEntity : BaseEntity<TKey>, new()
        {
            var tableName = typeof(TEntity).Name.Replace("Entity", "").Replace("Model", "");
            entity.ToTable(tableName).HasKey(x => x.Id);


            foreach (var propertyInfo in typeof(TEntity).GetProperties())
            {
                if (propertyInfo.Name == nameof(IIncludeCreateTime.CreateTime))
                {
                    entity.Property("CreateTime").HasColumnType("datetime");
                }

                if (propertyInfo.Name == nameof(IIncludeUpdateTime.UpdateTime))
                {
                    entity.Property("UpdateTime").HasColumnType("datetime");
                }

                if (propertyInfo.Name == nameof(IIncludeInitField.IsInitData))
                {
                    entity.Property("IsInitData").HasColumnType("bit");
                }

                if (propertyInfo.Name == "TenantId")
                {
                    if (propertyInfo.PropertyType == typeof(Guid))
                    {
                        entity.Property("TenantId").HasColumnType("varchar(36)");
                    }

                    if (propertyInfo.PropertyType == typeof(Int32))
                    {
                        entity.Property("TenantId").HasColumnType("number");
                    }

                    if (propertyInfo.PropertyType == typeof(string))
                    {
                        entity.Property("TenantId").HasColumnType("varchar(36)");
                    }
                }

                if (propertyInfo.Name == nameof(IIncludeDeleteField.IsDelete))
                {
                    entity.Property("IsDelete").HasColumnType("bit");
                }

                if (propertyInfo.Name == "CreatorId")
                {
                    if (propertyInfo.PropertyType == typeof(Guid))
                    {
                        entity.Property("TenantId").HasColumnType("varchar(36)");
                    }

                    if (propertyInfo.PropertyType == typeof(Int32))
                    {
                        entity.Property("TenantId").HasColumnType("number");
                    }

                    if (propertyInfo.PropertyType == typeof(string))
                    {
                        entity.Property("TenantId").HasColumnType("varchar(36)");
                    }
                }

                if (propertyInfo.Name == nameof(IIncludeCreatorName.CreatorName))
                {
                    entity.Property("CreatorName").HasColumnType("nvarchar(40)");
                }
            }
        }


        void CreateDataTable()
        {
            if (Database.GetService<IDatabaseCreator>() is RelationalDatabaseCreator databaseCreator)
            {
                databaseCreator.CreateTables();
            }
        }

        #endregion
    }
}