using System;
using Girvs.EntityFrameworkCore.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZhuoFan.Wb.BasicService.Domain.Models;

namespace ZhuoFan.Wb.BasicService.Infrastructure.EntityConfigurations
{
    public class ServiceDataRuleEntityTypeConfiguration: IEntityTypeConfiguration<ServiceDataRule>
    {
        public void Configure(EntityTypeBuilder<ServiceDataRule> builder)
        {
            GirvsDbContext.OnModelCreatingBaseEntityAndTableKey<ServiceDataRule,Guid>(builder);
            
            builder.Property(x => x.EntityTypeName).HasColumnType("varchar(200)");
            builder.Property(x => x.EntityDesc).HasColumnType("varchar(200)");
            builder.Property(x => x.FieldDesc).HasColumnType("varchar(50)");
            builder.Property(x => x.FieldName).HasColumnType("varchar(100)");
            builder.Property(x => x.FieldType).HasColumnType("varchar(100)");
            builder.Property(x => x.FieldValue).HasColumnType("varchar(10)");
        }
    }
}