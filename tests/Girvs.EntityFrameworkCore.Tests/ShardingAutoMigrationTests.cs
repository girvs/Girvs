using Girvs.BusinessBasis.Entities;
using Girvs.Configuration.Resources;
using Girvs.EntityFrameworkCore.Context;
using Girvs.EntityFrameworkCore.DbContextExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Girvs.EntityFrameworkCore.Tests;

[Collection(EfEngineCollection.Name)]
public class ShardingAutoMigrationTests
{
    [Fact]
    public void 同后缀不同上下文_分别迁移且重复调用只迁移一次()
    {
        using var scope = new EfTestScope();
        scope.Config.DataConnectionConfigs.Single().EnableShardingTable = true;
        using var first = Create<ShardContextA>(scope.Read);
        using var second = Create<ShardContextB>(scope.Read);

        first.ShardingAutoMigration();
        second.ShardingAutoMigration();
        first.ShardingAutoMigration();

        Assert.Equal(1, Probe(first).Calls);
        Assert.Equal(1, Probe(second).Calls);
        Assert.Equal(scope.Master, first.Database.GetConnectionString());
    }

    [Fact]
    public void 同上下文同后缀不同主库_分别迁移()
    {
        using var scope = new EfTestScope();
        var config = scope.Config.DataConnectionConfigs.Single();
        config.EnableShardingTable = true;
        using var first = Create<ShardContextA>(scope.Read);
        first.ShardingAutoMigration();
        var otherMaster = scope.Master.Replace("master.db", "another.db", StringComparison.Ordinal);
        config.ReadConnectionRefs = [];
        config.ResolveConnectionStrings(new Dictionary<string, GirvsInfrastructureResource>
        {
            ["master"] = new() { Type = "mysql", Settings = new Dictionary<string, string> { ["ConnectionString"] = otherMaster } },
        });
        using var second = Create<ShardContextA>(scope.Read);
        second.ShardingAutoMigration();
        Assert.Equal(1, Probe(first).Calls);
        Assert.Equal(1, Probe(second).Calls);
        Assert.Equal(otherMaster, second.Database.GetConnectionString());
    }

    [Fact]
    public void 迁移失败_保留异常且下一次允许重试()
    {
        using var scope = new EfTestScope();
        scope.Config.DataConnectionConfigs.Single().EnableShardingTable = true;
        using var context = Create<ShardContextA>(scope.Read);
        var probe = Probe(context);
        probe.OnMigrate = () => throw new InvalidOperationException("模拟迁移失败");
        Assert.Throws<InvalidOperationException>(() => context.ShardingAutoMigration());
        probe.OnMigrate = null;
        context.ShardingAutoMigration();
        context.ShardingAutoMigration();
        Assert.Equal(2, probe.Calls);
    }

    [Fact]
    public async Task 同迁移键并发_成功迁移只执行一次()
    {
        using var scope = new EfTestScope();
        scope.Config.DataConnectionConfigs.Single().EnableShardingTable = true;
        using var first = Create<ShardContextA>(scope.Read);
        using var second = Create<ShardContextA>(scope.Read);
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        Probe(first).OnMigrate = () =>
        {
            entered.Set();
            Assert.True(release.Wait(TimeSpan.FromSeconds(10)));
        };
        var firstTask = Task.Run(first.ShardingAutoMigration);
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(10)));
            var secondTask = Task.Run(second.ShardingAutoMigration);
            release.Set();
            await Task.WhenAll(firstTask, secondTask).WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal(1, Probe(first).Calls + Probe(second).Calls);
        }
        finally
        {
            release.Set();
            await firstTask;
        }
    }

    [Fact]
    public async Task 不同迁移键并发_慢迁移不会阻塞其他上下文()
    {
        using var scope = new EfTestScope();
        scope.Config.DataConnectionConfigs.Single().EnableShardingTable = true;
        using var first = Create<ShardContextA>(scope.Read);
        using var second = Create<ShardContextB>(scope.Read);
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        Probe(first).OnMigrate = () =>
        {
            entered.Set();
            Assert.True(release.Wait(TimeSpan.FromSeconds(10)));
        };
        var firstTask = Task.Run(first.ShardingAutoMigration);
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(10)));
            await Task.Run(second.ShardingAutoMigration).WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(1, Probe(second).Calls);
        }
        finally
        {
            release.Set();
            await firstTask;
        }
    }

    [Fact]
    public void 关闭分表或当前上下文无分表实体_不执行迁移()
    {
        using var scope = new EfTestScope();
        using var context = Create<ShardContextA>(scope.Read);
        context.ShardingAutoMigration();
        Assert.Equal(0, Probe(context).Calls);
        scope.Config.DataConnectionConfigs.Single().EnableShardingTable = true;
        scope.Context.ShardingAutoMigration();
        Assert.Equal(scope.Read, scope.Context.Database.GetConnectionString());
    }

    private static T Create<T>(string connection) where T : GirvsDbContext =>
        (T)Activator.CreateInstance(typeof(T), new DbContextOptionsBuilder()
            .UseSqlite(connection).ReplaceService<IMigrator, RecordingMigrator>().Options)!;

    private static RecordingMigrator Probe(DbContext context) => (RecordingMigrator)context.GetService<IMigrator>();
}

[GirvsDbConfig("test")]
public sealed class ShardContextA(DbContextOptions options) : GirvsDbContext(options)
{
    public DbSet<YearRow> Rows => Set<YearRow>();
}

[GirvsDbConfig("test")]
public sealed class ShardContextB(DbContextOptions options) : GirvsDbContext(options)
{
    public DbSet<YearRow> Rows => Set<YearRow>();
}

public sealed class YearRow : Entity<int>, IYearShardingTable
{
    public int Id { get; set; }
}

// 仅替换迁移执行器以观察协调行为，不将这些用例称为数据库迁移验收。
public sealed class RecordingMigrator : IMigrator
{
    public int Calls { get; private set; }
    public Action OnMigrate { get; set; }
    public void Migrate(string targetMigration = null)
    {
        Calls++;
        OnMigrate?.Invoke();
    }
    public Task MigrateAsync(string targetMigration = null, CancellationToken cancellationToken = default)
    {
        Migrate(targetMigration);
        return Task.CompletedTask;
    }
    public string GenerateScript(string fromMigration = null, string toMigration = null,
        MigrationsSqlGenerationOptions options = MigrationsSqlGenerationOptions.Default) => throw new NotSupportedException();
    public bool HasPendingModelChanges() => false;
}
