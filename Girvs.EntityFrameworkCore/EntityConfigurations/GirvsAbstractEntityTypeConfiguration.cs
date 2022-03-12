using System;
using System.Linq;
using Girvs.BusinessBasis.Entities;
using Girvs.EntityFrameworkCore.Configuration;
using Girvs.EntityFrameworkCore.Context;
using Girvs.EntityFrameworkCore.DbContextExtensions;
using Girvs.Extensions;
using Girvs.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Girvs.EntityFrameworkCore.EntityConfigurations
{
    public abstract class GirvsAbstractEntityTypeConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
        where TEntity : Entity
    {
        public virtual bool GetCurrentEnableShardingTable()
        {
            var context = EngineContext.Current.GetEntityRelatedDbContext<TEntity>();
            return EngineContext.Current.GetAppModuleConfig<DbConfig>().GetDataConnectionConfig(context.GetType())
                .IsTenantShardingTable;
        }

        public virtual string GetEntityTableName()
        {
            var tableName = typeof(TEntity).Name.Replace("Entity", "").Replace("Model", "");
            if (!GetCurrentEnableShardingTable())
            {
                return tableName;
            }

            var entityType = typeof(TEntity);
            if (entityType.IsAssignableTo(typeof(ITenantShardingTable)) &&
                entityType.GetProperties().Any(x => x.Name == nameof(IIncludeMultiTenant<object>.TenantId)))
            {
                if (EngineContext.Current.GetEntityRelatedDbContext<TEntity>() is not GirvsDbContext context ||
                    context.ShardingSuffix.IsNullOrEmpty())
                {
                    return tableName;
                }

                return $"{tableName}_{context.ShardingSuffix}";
            }

            return tableName;
        }

        public virtual void OnModelCreatingBaseEntityAndTableKey<TEntity, TKey>(EntityTypeBuilder<TEntity> builder)
            where TEntity : BaseEntity<TKey>, new()
        {
            builder.ToTable(GetEntityTableName()).HasKey(x => x.Id);
            foreach (var propertyInfo in typeof(TEntity).GetProperties())
            {
                if (propertyInfo.Name == nameof(IIncludeCreateTime.CreateTime))
                {
                    builder.Property(nameof(IIncludeCreateTime.CreateTime)).HasColumnType("datetime");
                }

                if (propertyInfo.Name == nameof(IIncludeUpdateTime.UpdateTime))
                {
                    builder.Property(nameof(IIncludeUpdateTime.UpdateTime)).HasColumnType("datetime");
                }

                if (propertyInfo.Name == nameof(IIncludeInitField.IsInitData))
                {
                    builder.Property(nameof(IIncludeInitField.IsInitData)).HasColumnType("bit");
                }

                if (propertyInfo.Name == nameof(IIncludeMultiTenant<object>.TenantId))
                {
                    if (propertyInfo.PropertyType == typeof(Guid))
                    {
                        builder.Property(nameof(IIncludeMultiTenant<object>.TenantId)).HasColumnType("varchar(36)");
                    }

                    if (propertyInfo.PropertyType == typeof(Int32))
                    {
                        builder.Property(nameof(IIncludeMultiTenant<object>.TenantId)).HasColumnType("number");
                    }

                    if (propertyInfo.PropertyType == typeof(string))
                    {
                        builder.Property(nameof(IIncludeMultiTenant<object>.TenantId)).HasColumnType("varchar(36)");
                    }
                }

                if (propertyInfo.Name == nameof(IIncludeDeleteField.IsDelete))
                {
                    builder.Property(nameof(IIncludeDeleteField.IsDelete)).HasColumnType("bit");
                }

                if (propertyInfo.Name == nameof(IIncludeCreatorId<object>.CreatorId))
                {
                    if (propertyInfo.PropertyType == typeof(Guid))
                    {
                        builder.Property(nameof(IIncludeCreatorId<object>.CreatorId)).HasColumnType("varchar(36)");
                    }

                    if (propertyInfo.PropertyType == typeof(Int32))
                    {
                        builder.Property(nameof(IIncludeCreatorId<object>.CreatorId)).HasColumnType("number");
                    }

                    if (propertyInfo.PropertyType == typeof(string))
                    {
                        builder.Property(nameof(IIncludeCreatorId<object>.CreatorId)).HasColumnType("varchar(36)");
                    }
                }

                if (propertyInfo.Name == nameof(IIncludeCreatorName.CreatorName))
                {
                    builder.Property(nameof(IIncludeCreatorName.CreatorName)).HasColumnType("nvarchar(40)");
                }
            }
        }

        public abstract void Configure(EntityTypeBuilder<TEntity> builder);
    }
}