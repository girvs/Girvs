﻿using BasicManagement.Domain.Models;
using Girvs.EntityFrameworkCore.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BasicManagement.Infrastructure.EntityConfigurations
{
    public class SysDictEntityTypeConfiguration : IEntityTypeConfiguration<SysDict>
    {
        public void Configure(EntityTypeBuilder<SysDict> builder)
        {
            GirvsDbContext.OnModelCreatingBaseEntityAndTableKey<SysDict, int>(builder);
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).UseMySqlIdentityColumn();
            builder.Property(x => x.CodeType).HasColumnType("nvarchar(36)");
            builder.Property(x => x.Code).HasColumnType("nvarchar(36)");
            builder.Property(x => x.Name).HasColumnType("nvarchar(36)");
            builder.Property(x => x.Desc).HasColumnType("nvarchar(200)");
        }
    }
}
