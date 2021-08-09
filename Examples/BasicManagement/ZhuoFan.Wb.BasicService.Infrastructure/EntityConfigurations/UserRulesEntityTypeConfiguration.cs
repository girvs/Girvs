using System;
using Girvs.EntityFrameworkCore.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZhuoFan.Wb.BasicService.Domain.Models;

namespace ZhuoFan.Wb.BasicService.Infrastructure.EntityConfigurations
{
    public class UserRulesEntityTypeConfiguration : IEntityTypeConfiguration<UserRules>
    {
        public void Configure(EntityTypeBuilder<UserRules> builder)
        {
            GirvsDbContext.OnModelCreatingBaseEntityAndTableKey<UserRules, Guid>(builder);

            builder.Property(x => x.Operate).HasColumnType("varchar(30)");
            builder.Property(x => x.FieldName).HasColumnType("varchar(50)");
            builder.Property(x => x.FieldValue).HasColumnType("varchar(1024)");
            builder.Property(x => x.ModuleName).HasColumnType("varchar(50)");
            builder.Property(x => x.UserType).HasColumnType("int");
        }
    }
}