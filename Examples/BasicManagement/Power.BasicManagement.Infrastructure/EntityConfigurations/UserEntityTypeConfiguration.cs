using System;
using Girvs.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Power.BasicManagement.Domain.Models;

namespace Power.BasicManagement.Infrastructure.EntityConfigurations
{
    public class UserEntityTypeConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            GirvsDbContext.OnModelCreatingBaseEntityAndTableKey<User,Guid>(builder);
            builder.Property(x => x.UserAccount).HasColumnType("varchar(36)");
            builder.Property(x => x.UserPassword).HasColumnType("varchar(36)");
            builder.Property(x => x.OtherId).HasColumnType("varchar(36)");
            builder.Property(x => x.UserName).HasColumnType("nvarchar(20)");
            builder.Property(x => x.ContactNumber).HasColumnType("varchar(12)");
            builder.Property(x => x.State).HasColumnType("int");
            builder.Property(x => x.UserType).HasColumnType("int");

            //添加用户种子数据
            //builder.HasData(new User()
            //{
            //    Id = Guid.Parse("58205e0e-1552-4282-bedc-a92d0afb37df"),
            //    UserName = "系统管理员",
            //    UserPassword = "21232F297A57A5A743894A0E4A801FC3",
            //    UserAccount = "admin",
            //    UserType = UserType.SuperAdmin,
            //    State = DataState.Enable,
            //    IsInitData = true
            //});
        }
    }
}