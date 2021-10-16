using System;
using System.Collections.Generic;
using System.Text.Json;
using Girvs.AuthorizePermission;
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
            var permissionConverter = new ValueConverter<Dictionary<string, string>, string>(
                v => JsonSerializerPermissionsString(v),
                v => JsonSerializerDeserializePermissions(v));
            
            
            var operationPermissionConverter = new ValueConverter<List<OperationPermissionModel>, string>(
                v => JsonSerializerOperationPermissionString(v),
                v => JsonSerializerDeserializeOperationPermission(v));

            GirvsDbContext.OnModelCreatingBaseEntityAndTableKey<ServicePermission, Guid>(builder);
            builder.Property(x => x.ServiceId).HasColumnType("varchar(36)");
            builder.Property(x => x.Permissions)
                .HasColumnType("text")
                .HasConversion(permissionConverter);
            builder.Property(x=>x.OperationPermissions)
                .HasColumnType("text")
                .HasConversion(operationPermissionConverter);
            builder.Property(x => x.ServiceName).HasColumnType("varchar(255)");
        }


        private string JsonSerializerPermissionsString(Dictionary<string, string> v)
        {
            return JsonSerializer.Serialize(v);
        }

        private Dictionary<string, string> JsonSerializerDeserializePermissions(string str)
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(str);
        }
        
        
        private string JsonSerializerOperationPermissionString(List<OperationPermissionModel> v)
        {
            return JsonSerializer.Serialize(v);
        }

        private List<OperationPermissionModel> JsonSerializerDeserializeOperationPermission(string str)
        {
            return JsonSerializer.Deserialize<List<OperationPermissionModel>>(str);
        }
    }
}