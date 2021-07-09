using System;
using Girvs.EntityFrameworkCore.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZhuoFan.Wb.BasicService.Domain.Models;

namespace ZhuoFan.Wb.BasicService.Infrastructure.EntityConfigurations
{
    public class RoleEntityTypeConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            GirvsDbContext.OnModelCreatingBaseEntityAndTableKey<Role,Guid>(builder);
            builder.Property(x => x.Name).HasColumnType("nvarchar(30)");
            builder.Property(x => x.Desc).HasColumnType("nvarchar(200)");


            //builder.HasData(new Role()
            //{
            //    Id = Guid.Parse("70ecc373-16f7-42e9-b31b-e80507b7c20a"),
            //    Name = "租户角色",
            //    Desc = "所有租户管理员具有的该角色",
            //    IsInitData = true
            //});
        }
    }
}