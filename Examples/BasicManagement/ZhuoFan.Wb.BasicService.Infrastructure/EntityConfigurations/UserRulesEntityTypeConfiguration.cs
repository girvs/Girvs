using System;
using Girvs.EntityFrameworkCore.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZhuoFan.Wb.BasicService.Domain.Models;

namespace ZhuoFan.Wb.BasicService.Infrastructure.EntityConfigurations
{
    public class UserRulesEntityTypeConfiguration : IEntityTypeConfiguration<UserRule>
    {
        public void Configure(EntityTypeBuilder<UserRule> builder)
        {
            GirvsDbContext.OnModelCreatingBaseEntityAndTableKey<UserRule, Guid>(builder);

            builder.Property(x => x.EntityTypeName).HasColumnType("varchar(200)");
            builder.Property(x => x.EntityDesc).HasColumnType("varchar(200)");
            builder.Property(x => x.FieldDesc).HasColumnType("varchar(50)");
            builder.Property(x => x.FieldName).HasColumnType("varchar(100)");
            builder.Property(x => x.FieldType).HasColumnType("varchar(100)");
            builder.Property(x => x.FieldValue).HasColumnType("text");
            builder.Property(x => x.FieldValueText).HasColumnType("text");
        }
    }
}