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
            
            builder.Property(x => x.DataSource).HasColumnType("varchar(500)");
            builder.Property(x => x.FieldDesc).HasColumnType("varchar(50)");
            builder.Property(x => x.FieldName).HasColumnType("varchar(50)");
            builder.Property(x => x.ModuleName).HasColumnType("varchar(50)");
            builder.Property(x => x.ServiceName).HasColumnType("varchar(100)");
            builder.Property(x => x.UserType).HasColumnType("int");
        }
    }
}