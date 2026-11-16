using Girvs;
using System.Data.Common;
using Girvs.BusinessBasis.Repositories;
using Girvs.Configuration.Resources;
using Girvs.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Girvs.EntityFrameworkCore.Tests;

[Collection(EfEngineCollection.Name)]
public class RepositoryBulkOperationTests
{
    [Theory]
    [InlineData("mysql")]
    [InlineData("sqlserver")]
    public async Task 批量更新_真实提供程序正确翻译动态字段且不建立网络连接(string provider)
    {
        using var scope = new EfTestScope();
        var connection = provider == "mysql"
            ? "Server=127.0.0.1;Database=GirvsSqlProbe;User=probe;Password=unused"
            : "Server=127.0.0.1;Database=GirvsSqlProbe;Integrated Security=true;TrustServerCertificate=true";
        var config = scope.Config.DataConnectionConfigs.Single();
        config.ReadConnectionRefs = [];
        config.ResolveConnectionStrings(new Dictionary<string, GirvsInfrastructureResource>
        {
            ["master"] = new() { Type = provider, Settings = new Dictionary<string, string> { ["ConnectionString"] = connection } },
        });
        var options = new DbContextOptionsBuilder<BulkDbContext>()
            .AddInterceptors(new SuppressConnectionOpen(), new CaptureCommand());
        if (provider == "mysql") options.UseMySql(connection, new MySqlServerVersion(new Version(8, 0, 36)));
        else options.UseSqlServer(connection);
        using var context = new BulkDbContext(options.Options);
        using var services = new ServiceCollection().AddLogging().AddSingleton(context)
            .AddSingleton(EngineContext.Current.Resolve<IRepositoryOtherQueryCondition>()).BuildServiceProvider();
        using var providerScope = EngineContext.Current.ChangeCurrentThreadServiceProvider(services);

        var exception = await Assert.ThrowsAsync<CapturedSqlException>(() => new BulkRepository()
            .UpdateRangeAsync(row => row.Id == 1, new KeyValuePair<string, object>("Name", "updated"), new("Count", 42)));

        Assert.Contains("UPDATE", exception.Sql);
        Assert.Contains("SET", exception.Sql);
        Assert.Contains("WHERE", exception.Sql);
        Assert.Contains(provider == "mysql" ? "`Name`" : "[Name]", exception.Sql);
        Assert.Contains(provider == "mysql" ? "`TenantId`" : "[TenantId]", exception.Sql);
    }

    [Fact]
    public async Task 批量更新_支持多类型与影子属性并保留租户过滤()
    {
        using var scope = new EfTestScope();
        var repository = new BulkRepository();

        await repository.UpdateRangeAsync(row => row.Id > 0,
            new KeyValuePair<string, object>("Name", "updated"), new KeyValuePair<string, object>("Count", 42), new KeyValuePair<string, object>("OptionalCount", 7),
            new KeyValuePair<string, object>("Note", null), new KeyValuePair<string, object>("Status", RowStatus.Done), new KeyValuePair<string, object>("ShadowValue", "shadow"));

        using var master = EfTestScope.CreateContext(scope.Master);
        var row = await master.Rows.SingleAsync(row => row.Id == 1);
        Assert.Equal("updated", row.Name);
        Assert.Equal(42, row.Count);
        Assert.Equal(7, row.OptionalCount);
        Assert.Null(row.Note);
        Assert.Equal(RowStatus.Done, row.Status);
        Assert.Equal("shadow", master.Entry(row).Property("ShadowValue").CurrentValue);
        Assert.Equal("other", (await master.Rows.SingleAsync(row => row.Id == 2)).Name);
        using var read = EfTestScope.CreateContext(scope.Read);
        Assert.Equal("first", (await read.Rows.SingleAsync(row => row.Id == 1)).Name);
    }

    [Theory]
    [InlineData("Missing", "value")]
    [InlineData("Unmapped", "value")]
    [InlineData("Id", 5)]
    [InlineData("TenantId", 2)]
    [InlineData("Count", "wrong")]
    [InlineData("Count", null)]
    [InlineData("Name", null)]
    [InlineData(" ", 1)]
    public async Task 批量更新_非法字段和值在执行前拒绝(string field, object value)
    {
        using var scope = new EfTestScope();
        var repository = new BulkRepository();

        await Assert.ThrowsAsync<GirvsException>(() => repository.UpdateRangeAsync(row => true,
            new KeyValuePair<string, object>("Name", "must-not-change"), new(field, value)));

        Assert.Equal(scope.Read, scope.Context.Database.GetConnectionString());
        using var master = EfTestScope.CreateContext(scope.Master);
        Assert.Equal("first", (await master.Rows.SingleAsync(row => row.Id == 1)).Name);
    }

    [Fact]
    public async Task 批量更新_重复字段拒绝_空字段和空结果无副作用()
    {
        using var scope = new EfTestScope();
        var repository = new BulkRepository();
        await Assert.ThrowsAsync<GirvsException>(() => repository.UpdateRangeAsync(row => true,
            new KeyValuePair<string, object>("Name", "one"), new KeyValuePair<string, object>("Name", "two")));
        await repository.UpdateRangeAsync(row => true, Array.Empty<KeyValuePair<string, object>>());
        Assert.Equal(scope.Read, scope.Context.Database.GetConnectionString());
        await repository.UpdateRangeAsync(row => row.Id == 999, new KeyValuePair<string, object>("Name", "missing"));
        using var master = EfTestScope.CreateContext(scope.Master);
        Assert.Equal(2, await master.Rows.CountAsync());
    }

    [Fact]
    public async Task 批量更新_可空值类型支持写入null()
    {
        using var scope = new EfTestScope();
        var repository = new BulkRepository();
        await repository.UpdateRangeAsync(row => row.Id == 1, new KeyValuePair<string, object>("OptionalCount", 9));
        await repository.UpdateRangeAsync(row => row.Id == 1, new KeyValuePair<string, object>("OptionalCount", null));
        using var master = EfTestScope.CreateContext(scope.Master);
        Assert.Null((await master.Rows.SingleAsync(row => row.Id == 1)).OptionalCount);
    }

    private sealed class CapturedSqlException(string sql) : Exception
    {
        public string Sql { get; } = sql;
    }

    private sealed class CaptureCommand : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            => throw new CapturedSqlException(command.CommandText);
    }

    private sealed class SuppressConnectionOpen : DbConnectionInterceptor
    {
        public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData,
            InterceptionResult result) => InterceptionResult.Suppress();
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection,
            ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(InterceptionResult.Suppress());
    }
}
