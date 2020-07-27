using Girvs.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Test.Domain.Models;

namespace Test.Infrastructure.EntityConfigurations
{
    public class RoleEntityTypeConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            ScsDbContext.OnModelCreatingBaseEntityAndTableKey(builder);
            builder.Property(x => x.Name).HasColumnType("nvarchar(20)");
            builder.Property(x => x.Desc).HasColumnType("nvarchar(200)");
        }
    }
}