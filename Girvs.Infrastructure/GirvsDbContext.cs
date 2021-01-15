using System;
using Girvs.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Girvs.Infrastructure
{
    public abstract class GirvsDbContext : DbContext, IDbContext
    {
        protected GirvsDbContext(DbContextOptions options)
            : base(options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //void Action(DbContextOptionsBuilder builder)
            //{
            //    switch (config.UseDataType)
            //    {
            //        case UseDataType.MsSql:
            //            builder.UseSqlServerWithLazyLoading(services);
            //            break;

            //        case UseDataType.MySql:
            //            builder.UseMySqlWithLazyLoading(services);
            //            break;

            //        case UseDataType.SqlLite:
            //            builder.UseSqlLiteWithLazyLoading(services);
            //            break;

            //        case UseDataType.Oracle:
            //            builder.UseOracleWithLazyLoading(services);
            //            break;
            //    }

            //    if (!config.UseDataTracking)
            //    {
            //        builder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            //    }

            //    builder.EnableSensitiveDataLogging();
            //}
            base.OnConfiguring(optionsBuilder);
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