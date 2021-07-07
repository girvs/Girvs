using BasicManagement.Domain.Models;
using BasicManagement.Infrastructure.EntityConfigurations;
using Girvs.EntityFrameworkCore.Context;
using Microsoft.EntityFrameworkCore;

namespace BasicManagement.Infrastructure
{
    public class BasicManagementDbContext : GirvsDbContext
    {
        public BasicManagementDbContext(DbContextOptions<BasicManagementDbContext> options) : base(options)
        {
            
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }

        public override string DbConfigName { get; set; } = "BasicManagementDataConnection";

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(GetDbConnectionString());
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new RoleEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new UserEntityTypeConfiguration());
        }

        //public override string GetDbConnectionString()
        //{
        //    return "Data Source=192.168.51.188;Initial Catalog=kicckTest;User Id=sa;Password=123456";
        //}
        

    }
}