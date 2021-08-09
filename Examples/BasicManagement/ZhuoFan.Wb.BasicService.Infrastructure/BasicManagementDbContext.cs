using Girvs.EntityFrameworkCore.Context;
using Microsoft.EntityFrameworkCore;
using ZhuoFan.Wb.BasicService.Domain.Models;
using ZhuoFan.Wb.BasicService.Infrastructure.EntityConfigurations;

namespace ZhuoFan.Wb.BasicService.Infrastructure
{
    public class BasicManagementDbContext : GirvsDbContext
    {
        public BasicManagementDbContext(DbContextOptions<BasicManagementDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<BasalPermission> BasalPermissions { get; set; }
        public DbSet<ServicePermission> ServicePermissions { get; set; }
        public DbSet<ServiceDataRule> ServiceDataRules { get; set; }
        public DbSet<UserRules> UserRulesEnumerable { get; set; }

        public override string DbConfigName { get; set; } = "BasicManagementDataConnection";

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new RoleEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new UserEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new BasalPermissionEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new UserRulesEntityTypeConfiguration());

            modelBuilder.ApplyConfiguration(new ServicePermissionEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new ServiceDataRuleEntityTypeConfiguration());
        }
    }
}