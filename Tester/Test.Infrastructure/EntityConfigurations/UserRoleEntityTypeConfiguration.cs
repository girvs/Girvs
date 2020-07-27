using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Test.Domain.Models;

namespace Test.Infrastructure.EntityConfigurations
{
    public class UserRoleEntityTypeConfiguration:IEntityTypeConfiguration<UserRole>
    {
        public void Configure(EntityTypeBuilder<UserRole> builder)
        {
            builder.ToTable($"UserRole").HasKey(ur => new {ur.UserId, ur.RoleId});
            builder.HasOne(x => x.User).WithMany(x => x.UserRoles);
            builder.HasOne(x => x.Role).WithMany(x => x.UserRoles);
        }
    }
}