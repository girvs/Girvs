using System.Threading.Tasks;
using Girvs.Domain.Managers;
using Girvs.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Test.Domain.Models;
using Test.Infrastructure.EntityConfigurations;

namespace Test.Infrastructure
{
    public class CmmpDbContext : ScsDbContext, IUnitOfWork
    {
        public CmmpDbContext(DbContextOptions<CmmpDbContext> options) : base(options)
        {
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

        public async Task<bool> Commit()
        {
            return await this.SaveChangesAsync() > 0;
        }
    }
}