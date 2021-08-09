using System;
using System.Collections.Generic;
using System.Text.Json;
using Girvs.EntityFrameworkCore.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ZhuoFan.Wb.BasicService.Domain.Models;

namespace ZhuoFan.Wb.BasicService.Infrastructure.EntityConfigurations
{
    public class ServicePermissionEntityTypeConfiguration : IEntityTypeConfiguration<ServicePermission>
    {
        public void Configure(EntityTypeBuilder<ServicePermission> builder)
        {
            var converter = new ValueConverter<Dictionary<string, string>, string>(
                v => JsonSerializerString(v),
                v => JsonSerializerDeserialize(v));

            GirvsDbContext.OnModelCreatingBaseEntityAndTableKey<ServicePermission, Guid>(builder);
            builder.Property(x => x.ServiceId).HasColumnType("varchar(36)");
            builder.Property(x => x.Permissions).HasColumnType("text").HasConversion(converter);
            builder.Property(x => x.ServiceName).HasColumnType("varchar(255)");
        }


        private string JsonSerializerString(Dictionary<string, string> v)
        {
            return JsonSerializer.Serialize(v);
        }

        private Dictionary<string, string> JsonSerializerDeserialize(string str)
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(str);
        }
    }
}