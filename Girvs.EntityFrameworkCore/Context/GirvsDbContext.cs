namespace Girvs.EntityFrameworkCore.Context;

public abstract class GirvsDbContext : DbContext
{
    public GirvsDbContext(DbContextOptions options) : base(options)
    {

    }
}