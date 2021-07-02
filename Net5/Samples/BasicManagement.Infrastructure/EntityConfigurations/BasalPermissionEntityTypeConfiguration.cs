using System;
using BasicManagement.Domain.Models;
using Girvs.EntityFrameworkCore.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BasicManagement.Infrastructure.EntityConfigurations
{
    public class BasalPermissionEntityTypeConfiguration : IEntityTypeConfiguration<BasalPermission>
    {
        public void Configure(EntityTypeBuilder<BasalPermission> builder)
        {
            GirvsDbContext.OnModelCreatingBaseEntityAndTableKey<BasalPermission, Guid>(builder);
            builder.Property(x => x.AppliedObjectType).HasColumnType("int");
            builder.Property(x => x.ValidateObjectType).HasColumnType("int");
            builder.Property(x => x.DenyMask).HasColumnType("bigint");
            builder.Property(x => x.AllowMask).HasColumnType("bigint");
        }
    }
}