using System;
using Girvs.Domain;
using Girvs.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Girvs.Infrastructure
{
    public abstract class ScsDbContext : DbContext
    {
        private readonly string _tableNamePrefix;

        public ScsDbContext(DbContextOptions options, string tableNamePrefix)
            : base(options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (tableNamePrefix == null) throw new ArgumentNullException(nameof(tableNamePrefix));
            _tableNamePrefix = tableNamePrefix + "_";
        }

        public virtual void OnModelCreatingBaseEntityAndTableKey<T>(EntityTypeBuilder<T> entity)
            where T : BaseEntity, new()
        {
            string tableName = typeof(T).Name.Replace("Entity", "");
            tableName = $"{_tableNamePrefix}{tableName}";
            entity.ToTable(tableName).HasKey(x => x.Id);
            //entity.Property(x => x.Id).HasColumnType("varchar(36)");
            // entity.Property(x => x.TenantId).HasColumnType("varchar(36)");
            // entity.Property(x => x.Creator).HasColumnType("varchar(36)");
            entity.Property(x => x.CreateTime).HasColumnType("datetime");
            entity.Property(x => x.UpdateTime).HasColumnType("datetime");
        }
    }
}