using Girvs.EntityFrameworkCore.Context;
using Girvs.EntityFrameworkCore.DbContextExtensions;
using Microsoft.EntityFrameworkCore;
using Sample.ServiceA.Entities;

namespace Sample.ServiceA.Data;

// [GirvsDbConfig] 的名字必须等于 appsettings 的 DbConfig.DataConnectionConfigs[].Name；
// 该 DbContext 在服务程序集内，会被 AddGirvsObjectContext 反射自动发现并注册。
[GirvsDbConfig("default")]
public class SampleDbContext(DbContextOptions<SampleDbContext> options) : GirvsDbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
}
