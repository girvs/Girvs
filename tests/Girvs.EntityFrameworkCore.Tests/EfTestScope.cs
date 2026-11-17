using System.Linq.Expressions;
using Girvs;
using Girvs.BusinessBasis.Entities;
using Girvs.BusinessBasis.Repositories;
using Girvs.Configuration;
using Girvs.Configuration.Resources;
using Girvs.EntityFrameworkCore.Configuration;
using Girvs.EntityFrameworkCore.Context;
using Girvs.EntityFrameworkCore.DbContextExtensions;
using Girvs.EntityFrameworkCore.Repositories;
using Girvs.Infrastructure;
using Girvs.FileProvider;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace Girvs.EntityFrameworkCore.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class EfEngineCollection
{
    public const string Name = "EF 引擎上下文";
}

internal sealed class EfTestScope : IDisposable
{
    private readonly IEngine _previousEngine = Singleton<IEngine>.Instance;
    private readonly AppSettings _previousSettings = Singleton<AppSettings>.Instance;
    private readonly IGirvsFileProvider _previousFileProvider = CommonHelper.DefaultFileProvider;
    private readonly GirvsFileProvider _fileProvider;
    private readonly IDisposable _providerScope;
    private readonly ServiceProvider _provider;
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "GirvsEfTests-" + Guid.NewGuid().ToString("N"));

    public string Master { get; }
    public string Read { get; }
    public DbConfig Config { get; }
    public BulkDbContext Context { get; }

    public EfTestScope(Action<DbContextOptionsBuilder<BulkDbContext>> configure = null)
    {
        Directory.CreateDirectory(_directory);
        _fileProvider = new GirvsFileProvider(new TestEnvironment());
        CommonHelper.DefaultFileProvider = _fileProvider;
        Master = $"Data Source={Path.Combine(_directory, "master.db")};Pooling=False";
        Read = $"Data Source={Path.Combine(_directory, "read.db")};Pooling=False";
        var connection = new DataConnectionConfig
        {
            Name = "test", ConnectionRef = ["master"], ReadConnectionRefs = [["read"]],
            EnableShardingTable = false,
        };
        Config = new DbConfig { DataConnectionConfigs = [connection] };
        var settings = new AppSettings
        {
            Resources = new Dictionary<string, GirvsInfrastructureResource>
            {
                ["master"] = Resource(Master), ["read"] = Resource(Read),
            },
        };
        settings.PreLoadModelConfig();
        settings[nameof(DbConfig)] = Config;
        Config.ResolveConnectionStrings(settings.Resources);
        Singleton<AppSettings>.Instance = settings;
        var engine = new GirvsEngine();
        EngineContext.Replace(engine);

        var options = new DbContextOptionsBuilder<BulkDbContext>().UseSqlite(Read);
        configure?.Invoke(options);
        Context = new BulkDbContext(options.Options);
        _provider = new ServiceCollection().AddLogging()
            .AddSingleton(Context)
            .AddSingleton<IRepositoryOtherQueryCondition, TenantQueryCondition>()
            .AddSingleton<IGirvsPrincipalAccessor>(new GirvsPrincipalAccessor(new Microsoft.AspNetCore.Http.HttpContextAccessor
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                {
                    User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity([
                        new System.Security.Claims.Claim(GirvsClaimTypes.TenantId, "1")
                    ]))
                }
            }))
            .BuildServiceProvider();
        _providerScope = engine.ChangeCurrentThreadServiceProvider(_provider);

        Seed(Master);
        Seed(Read);
    }

    private static GirvsInfrastructureResource Resource(string connection) => new()
    {
        // 仅复用 Girvs 资源解析；SQLite 是测试专用关系提供程序，不新增生产配置类型。
        Type = "mysql", Settings = new Dictionary<string, string> { ["ConnectionString"] = connection },
    };

    public static BulkDbContext CreateContext(string connection) =>
        new(new DbContextOptionsBuilder<BulkDbContext>().UseSqlite(connection).Options);

    private static void Seed(string connection)
    {
        using var context = CreateContext(connection);
        context.Database.EnsureCreated();
        context.Rows.AddRange(
            new BulkRow { Id = 1, TenantId = 1, Name = "first", Count = 10, Status = RowStatus.New },
            new BulkRow { Id = 2, TenantId = 2, Name = "other", Count = 20, Status = RowStatus.New });
        context.SaveChanges();
    }

    public void Dispose()
    {
        _providerScope.Dispose();
        Context.Dispose();
        _provider.Dispose();
        Singleton<AppSettings>.Instance = _previousSettings;
        Singleton<IEngine>.Instance = _previousEngine;
        CommonHelper.DefaultFileProvider = _previousFileProvider;
        _fileProvider.Dispose();
        // 临时目录由本测试创建，仅删除其中已知文件，不递归删除。
        foreach (var path in Directory.EnumerateFiles(_directory)) File.Delete(path);
        Directory.Delete(_directory);
    }

    private sealed class TenantQueryCondition : IRepositoryOtherQueryCondition
    {
        public bool ContainsPublicData { get; set; }
        public Expression<Func<TEntity, bool>> GetOtherQueryCondition<TEntity>() where TEntity : Entity =>
            entity => EF.Property<int>(entity, nameof(BulkRow.TenantId)) == 1;
    }


    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Girvs.EntityFrameworkCore.Tests";
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}

[GirvsDbConfig("test")]
public sealed class BulkDbContext(DbContextOptions<BulkDbContext> options) : GirvsDbContext(options)
{
    public DbSet<BulkRow> Rows => Set<BulkRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BulkRow>().Property(row => row.Name).IsRequired();
        modelBuilder.Entity<BulkRow>().Property(row => row.Note).IsRequired(false);
        modelBuilder.Entity<BulkRow>().Property<string>("ShadowValue");
        modelBuilder.Entity<BulkRow>().Property(row => row.Version).IsConcurrencyToken();
        modelBuilder.Entity<BulkRow>().ToTable(nameof(BulkRow));
    }
}

public enum RowStatus { New, Done }

public sealed class BulkRow : Entity<int>, IIncludeMultiTenant<int>, IIncludeUpdateTime
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public int? OptionalCount { get; set; }
    public string Note { get; set; }
    public RowStatus Status { get; set; }
    public DateTime UpdateTime { get; set; }
    public int Version { get; set; } = 1;
    public string Unmapped => Name;
}

public sealed class BulkRepository : Repository<BulkRow, int>;
