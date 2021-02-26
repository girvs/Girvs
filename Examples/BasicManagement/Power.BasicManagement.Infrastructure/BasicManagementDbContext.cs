using Girvs.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Power.BasicManagement.Domain.Models;
using Power.BasicManagement.Infrastructure.EntityConfigurations;

namespace Power.BasicManagement.Infrastructure
{
    public class BasicManagementDbContext : GirvsDbContext
    {
        public BasicManagementDbContext(DbContextOptions<BasicManagementDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<BasalPermission> BasePermissions { get; set; }
        public DbSet<SysDict> SysDicts { get; set; }

//#if DEBUG
//        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//        {

//        }
//#endif
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new BasalPermissionEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new RoleEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new UserEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new UserRoleEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new SysDictEntityTypeConfiguration());
        }

        public override string DbConfigName { get; } = "BasicManagementDataConnection";
    }
}