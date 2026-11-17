using System.Reflection;
using System.Data.Common;
using Girvs.EntityFrameworkCore.Context;
using Girvs.EntityFrameworkCore.DbContextExtensions;
using Girvs.Extensions.Collections;
using Girvs.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Girvs.EntityFrameworkCore.Tests;

public class ProviderCompatibilityTests
{
    [Theory]
    [InlineData("mysql")]
    [InlineData("sqlserver")]
    public void 查询_使用真实提供程序生成过滤SQL(string provider)
    {
        using var context = CreateContext(provider);

        var sql = context.Set<ProbeEntity>()
            .Where(entity => entity.Id == 7 && entity.Name == "probe")
            .Select(entity => entity.Name)
            .ToQueryString();

        Assert.Contains("SELECT", sql);
        Assert.Contains("WHERE", sql);
        Assert.Contains("7", sql);
        Assert.Contains("probe", sql);
        Assert.Contains(provider == "mysql" ? "`Name`" : "[Name]", sql);
    }

    [Theory]
    [InlineData("mysql")]
    [InlineData("sqlserver")]
    public async Task 动态筛选和分页_参数化且支持未投影排序字段(string provider)
    {
        using var context = CreateContext(provider, new SuppressOpen(), new CaptureRead());
        var exception = await Assert.ThrowsAsync<ReadCapturedException>(() => context.Set<ProbeEntity>().WhereEqual(nameof(ProbeEntity.Name), "probe")
            .OrderByDescending(entity => entity.Id).Skip(1).Take(2)
            .SelectProperties([nameof(ProbeEntity.Name)]).ToListAsync());
        var sql = exception.Sql;
        Assert.Contains("ORDER BY", sql);
        Assert.Contains("@", sql[sql.IndexOf("WHERE", StringComparison.Ordinal)..]);
        Assert.DoesNotContain("= 'probe'", sql[sql.IndexOf("WHERE", StringComparison.Ordinal)..]);
        Assert.Contains("probe", exception.Parameters);
    }

    [Theory]
    [InlineData("mysql")]
    [InlineData("sqlserver")]
    public void 创建脚本_正确引用含特殊字符的索引名(string provider)
    {
        using var context = CreateContext(provider);

        var script = context.Database.GenerateCreateScript();

        Assert.Contains("CREATE TABLE", script);
        Assert.Contains(provider == "mysql" ? "`IX_Probe_Name~`" : "[IX_Probe_Name~]", script);
    }

#if NET11_0_OR_GREATER
    [Theory]
    [InlineData("mysql")]
    [InlineData("sqlserver")]
    public void 迁移快照_保留EFCore11最新迁移标识(string provider)
    {
        using var context = CreateContext(provider);
        var services = new ServiceCollection()
            .AddEntityFrameworkDesignTimeServices()
            .AddDbContextDesignTimeServices(context);
        var providerAssembly = Assembly.Load(context.Database.ProviderName!);
        var providerServices = providerAssembly.GetCustomAttribute<DesignTimeProviderServicesAttribute>();
        Assert.NotNull(providerServices);
        var providerServicesType = providerAssembly.GetType(providerServices.TypeName)!;
        ((IDesignTimeServices)Activator.CreateInstance(providerServicesType)!).ConfigureDesignTimeServices(services);
        using var serviceProvider = services.BuildServiceProvider();
        var generator = ActivatorUtilities.CreateInstance<GirvsCSharpMigrationsGenerator>(serviceProvider);

        var snapshot = generator.GenerateSnapshot(
            "GirvsProviderProbe.Migrations",
            typeof(ProbeDbContext),
            "ProbeSnapshot",
            context.GetService<IDesignTimeModel>().Model,
            "202611130001_ProviderProbe");

        Assert.Contains("202611130001_ProviderProbe", snapshot);
        Assert.Contains("LastMigrationId", snapshot);
        Assert.Contains("ProbeSnapshot", snapshot);
    }
#endif

    private static ProbeDbContext CreateContext(string provider, params IInterceptor[] interceptors)
    {
        var options = new DbContextOptionsBuilder<ProbeDbContext>().AddInterceptors(interceptors);
        if (provider == "mysql")
        {
            options.UseMySql(
                "Server=127.0.0.1;Database=GirvsProviderProbe;User=probe;Password=unused",
                new MySqlServerVersion(new Version(8, 0, 36)));
        }
        else
        {
            options.UseSqlServer(
                "Server=127.0.0.1;Database=GirvsProviderProbe;Integrated Security=true;TrustServerCertificate=true");
        }

        return new ProbeDbContext(options.Options);
    }

    private sealed class ReadCapturedException(DbCommand command) : Exception
    {
        public string Sql { get; } = command.CommandText;
        public object[] Parameters { get; } = command.Parameters.Cast<DbParameter>().Select(parameter => parameter.Value).ToArray();
    }

    private sealed class CaptureRead : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default) =>
            throw new ReadCapturedException(command);
    }

    private sealed class SuppressOpen : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection,
            ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(InterceptionResult.Suppress());
    }

    public sealed class ProbeDbContext(DbContextOptions<ProbeDbContext> options) : GirvsDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<ProbeEntity>();
            entity.ToTable("ProviderProbe");
            entity.Property(probe => probe.Name).HasMaxLength(128);
            entity.HasIndex(probe => probe.Name).HasDatabaseName("IX_Probe_Name~");
        }
    }

    public class ProbeEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
