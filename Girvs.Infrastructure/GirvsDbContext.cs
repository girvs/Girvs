using System;
using System.Linq;
using Girvs.Domain;
using Girvs.Domain.Configuration;
using Girvs.Domain.Enumerations;
using Girvs.Domain.Infrastructure;
using Girvs.Domain.Models;
using Girvs.Infrastructure.DbContextExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Girvs.Infrastructure
{
    public abstract class GirvsDbContext : DbContext, IDbContext
    {
        public abstract string DbConfigName { get; }
        public void SetReadAndWriteMode(DataBaseWriteAndRead writeAndRead)
        {
            ReadAndWriteMode = writeAndRead;
        }

        public abstract DataBaseWriteAndRead ReadAndWriteMode { get; set; }
        protected GirvsDbContext(DbContextOptions options)
            : base(options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (string.IsNullOrEmpty(DbConfigName))
            {
                throw new GirvsException("DbContext未绑定指定的数据库名称", 568);
            }
            else
            {
                var config = EngineContext.Current.Resolve<GirvsConfig>();
                var dataConnectionConfig = config.DataConnectionConfigs.FirstOrDefault(x => x.Name == DbConfigName);
                if (dataConnectionConfig == null)
                {
                    throw new GirvsException("DbContext未绑定指定的数据库名称不正确", 568);
                }

                switch (dataConnectionConfig.UseDataType)
                {
                    case UseDataType.MsSql:
                        optionsBuilder.UseSqlServerWithLazyLoading(dataConnectionConfig, ReadAndWriteMode);
                        break;

                    case UseDataType.MySql:
                        optionsBuilder.UseMySqlWithLazyLoading(dataConnectionConfig, ReadAndWriteMode);
                        break;

                    case UseDataType.SqlLite:
                        optionsBuilder.UseSqlLiteWithLazyLoading(dataConnectionConfig, ReadAndWriteMode);
                        break;

                    case UseDataType.Oracle:
                        optionsBuilder.UseOracleWithLazyLoading(dataConnectionConfig, ReadAndWriteMode);
                        break;
                }

                if (!dataConnectionConfig.UseDataTracking)
                {
                    optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                }

                optionsBuilder.EnableSensitiveDataLogging();
            }
        }

        public static void OnModelCreatingBaseEntityAndTableKey<TEntity, TKey>(EntityTypeBuilder<TEntity> entity)
            where TEntity : BaseEntity<TKey>, new()
        {
            string tableName = typeof(TEntity).Name.Replace("Entity", "").Replace("Model", "");
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
    }
}