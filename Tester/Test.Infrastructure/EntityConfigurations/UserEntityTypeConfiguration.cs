using System;
using Girvs.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Test.Domain.Enumerations;
using Test.Domain.Models;

namespace Test.Infrastructure.EntityConfigurations
{
    public class UserEntityTypeConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            ScsDbContext.OnModelCreatingBaseEntityAndTableKey(builder);
            builder.Property(x => x.UserAccount).HasColumnType("nvarchar(36)");
            builder.Property(x => x.UserPassword).HasColumnType("nvarchar(36)");
            builder.Property(x => x.UserName).HasColumnType("nvarchar(20)");
            builder.Property(x => x.ContactNumber).HasColumnType("nvarchar(12)");
            builder.Property(x => x.State).HasColumnType("int");
            builder.Property(x => x.UserType).HasColumnType("int");
            
            //添加用户种子数据
            builder.HasData(new User()
            {
                Id = Guid.Parse("58205e0e-1552-4282-bedc-a92d0afb37df"),
                UserName = "系统管理员",
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now,
                UserPassword = "21232F297A57A5A743894A0E4A801FC3",
                UserAccount = "admin",
                Creator = Guid.Parse("58205e0e-1552-4282-bedc-a92d0afb37df"),
                UserType = UserType.SuperAdmin,
                TenantId = Guid.Parse("f339be29-7ce2-4876-bcca-d3abe3d16f75"),
                State = DataState.Enable
            });
        }
    }
}