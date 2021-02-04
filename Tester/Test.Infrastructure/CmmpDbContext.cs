using Girvs.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Test.Domain.Models;
using Test.Infrastructure.EntityConfigurations;

namespace Test.Infrastructure
{
    public class CmmpDbContext : GirvsDbContext{
        public override string DbConfigName { get; } = "CmmpDataConnection";
        public CmmpDbContext(DbContextOptions<CmmpDbContext> options) : base(options)
        {
            ModelTypes.Add(typeof(User));
            ModelTypes.Add(typeof(Role));
            ModelTypes.Add(typeof(UserRole));
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new RoleEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new UserEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new UserRoleEntityTypeConfiguration());
        }
    }
}