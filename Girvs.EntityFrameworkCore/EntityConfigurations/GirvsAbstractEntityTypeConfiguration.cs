using System;
using Girvs.BusinessBasis.Entities;
using Girvs.EntityFrameworkCore.DbContextExtensions;
using Girvs.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Girvs.EntityFrameworkCore.EntityConfigurations
{
    public abstract class GirvsAbstractEntityTypeConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
        where TEntity : Entity
    {
        public virtual void OnModelCreatingBaseEntityAndTableKey<TEntity, TKey>(EntityTypeBuilder<TEntity> builder)
            where TEntity : BaseEntity<TKey>, new()
        {
            builder.ToTable(EngineContext.Current.GetMigrationEntityTableName<TEntity>()).HasKey(x => x.Id);
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