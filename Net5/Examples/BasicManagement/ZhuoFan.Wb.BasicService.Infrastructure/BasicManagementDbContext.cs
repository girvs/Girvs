using Girvs.EntityFrameworkCore.Context;
using Microsoft.EntityFrameworkCore;
using ZhuoFan.Wb.BasicService.Domain.Models;
using ZhuoFan.Wb.BasicService.Infrastructure.EntityConfigurations;

namespace ZhuoFan.Wb.BasicService.Infrastructure
{
    public class BasicManagementDbContext : GirvsDbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }

        public override string DbConfigName { get; set; } = "BasicManagementDataConnection";

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new RoleEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new UserEntityTypeConfiguration());
        }
    }
}