using System.Linq.Expressions;
using System.Data.Common;
using Girvs;
using Girvs.BusinessBasis.Queries;
using Girvs.BusinessBasis.Repositories;
using Girvs.EntityFrameworkCore.DbContextExtensions;
using Girvs.EntityFrameworkCore.UoW;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace Girvs.EntityFrameworkCore.Tests;

[Collection(EfEngineCollection.Name)]
public class RepositoryPerformanceTests
{
    [Fact]
    public async Task 动态筛选_不同值复用查询编译且不串值()
    {
        var compilations = 0;
        using var scope = new EfTestScope(builder => builder.LogTo(_ => compilations++, [CoreEventId.QueryCompilationStarting]));
        var names = new[] { "first", "other", "missing" };
        foreach (var name in names)
        {
            var rows = await scope.Context.Rows.WhereEqual(nameof(BulkRow.Name), name).ToListAsync();
            Assert.All(rows, row => Assert.Equal(name, row.Name));
            Assert.Equal(name == "missing" ? 0 : 1, rows.Count);
        }
        Assert.Equal(1, compilations);
    }

    [Fact]
    public async Task 租户筛选_不同租户复用查询编译且隔离数据()
    {
        var compilations = 0;
        using var scope = new EfTestScope(builder => builder.LogTo(_ => compilations++, [CoreEventId.QueryCompilationStarting]));
        var condition = new TenantCondition();
        foreach (var tenant in new[] { 1, 2, 3 })
        {
            var rows = await scope.Context.Rows.Where(condition.ForTenant(tenant)).ToListAsync();
            Assert.All(rows, row => Assert.Equal(tenant, row.TenantId));
            Assert.Equal(tenant == 3 ? 0 : 1, rows.Count);
        }
        Assert.Equal(1, compilations);
    }

    [Fact]
    public async Task 分页_排序字段未投影时仍按原字段排序并保留租户条件()
    {
        using var scope = new EfTestScope();
        scope.Context.Rows.AddRange(
            new BulkRow { Id = 3, TenantId = 1, Name = "third", Count = 30 },
            new BulkRow { Id = 4, TenantId = 1, Name = "fourth", Count = 30 });
        await scope.Context.SaveChangesAsync();
        scope.Context.ChangeTracker.Clear();
        var query = new RowsQuery { QueryFields = ["Name"], OrderBy = "Count", PageSize = 2 };
        var rows = await new BulkRepository().GetByQueryAsync(query);
        Assert.Equal(3, query.RecordCount);
        Assert.Equal(new[] { "fourth", "third" }, rows.Select(row => row.Name));
    }

    [Fact]
    public async Task 局部更新_残缺实体仅更新指定字段()
    {
        using var scope = new EfTestScope();
        var repository = new BulkRepository();
        await repository.UpdateAsync(new BulkRow { Id = 1, TenantId = 1, Name = "changed" }, "Name");
        using var unit = new UnitOfWork<BulkRow>();
        Assert.True(await unit.Commit());
        using var master = EfTestScope.CreateContext(scope.Master);
        var row = await master.Rows.SingleAsync(row => row.Id == 1);
        Assert.Equal("changed", row.Name);
        Assert.Equal(10, row.Count);
    }

    [Fact]
    public async Task 跟踪更新_未指定字段时只标记实际变化的字段()
    {
        using var scope = new EfTestScope();
        var row = await scope.Context.Rows.SingleAsync(row => row.Id == 1);
        row.Name = "changed";
        await new BulkRepository().UpdateAsync(row);
        Assert.Equal(new[] { "Name", "UpdateTime" }, scope.Context.Entry(row).Properties
            .Where(property => property.IsModified).Select(property => property.Metadata.Name));
    }

    [Fact]
    public async Task 全部取消跟踪_使用Clear且不触发逐条状态事件()
    {
        using var scope = new EfTestScope();
        _ = await scope.Context.Rows.ToListAsync();
        var stateChanges = 0;
        scope.Context.ChangeTracker.StateChanged += (_, _) => stateChanges++;
        scope.Context.DetachAll();
        Assert.Empty(scope.Context.ChangeTracker.Entries());
        Assert.Equal(0, stateChanges);
    }

    [Fact]
    public async Task 无总数分页_仅执行一条命令且HasMore正确()
    {
        var commands = new ReadCommands();
        using var scope = new EfTestScope(builder => builder.AddInterceptors(commands));
        scope.Context.Rows.Add(new BulkRow { Id = 3, TenantId = 1, Name = "third" });
        await scope.Context.SaveChangesAsync();
        scope.Context.ChangeTracker.Clear();
        commands.Count = 0;
        var repository = new BulkRepository();
        var query = new RowsQuery { IncludeRecordCount = false, PageSize = 1, AsNoTracking = true };
        var first = await repository.GetByQueryAsync(query);
        Assert.Equal(1, commands.Count);
        Assert.Equal(-1, query.RecordCount);
        Assert.Equal(0, query.PageCount);
        Assert.True(query.HasMore);
        Assert.Equal(3, Assert.Single(first).Id);
        Assert.Empty(scope.Context.ChangeTracker.Entries());
        query.PageIndex = 2;
        Assert.Equal(1, Assert.Single(await repository.GetByQueryAsync(query)).Id);
        Assert.False(query.HasMore);
        query.PageIndex = 3;
        Assert.Empty(await repository.GetByQueryAsync(query));
        Assert.False(query.HasMore);
    }

    [Fact]
    public async Task 计数分页_保留默认跟踪且越界仅查询总数()
    {
        var commands = new ReadCommands();
        using var scope = new EfTestScope(builder => builder.AddInterceptors(commands));
        var repository = new BulkRepository();
        var query = new RowsQuery { PageSize = 1 };
        Assert.Single(await repository.GetByQueryAsync(query));
        Assert.Equal(2, commands.Count);
        Assert.Single(scope.Context.ChangeTracker.Entries());
        Assert.Equal(1, query.RecordCount);
        Assert.False(query.HasMore);
        commands.Count = 0;
        query.PageIndex = 2;
        Assert.Empty(await repository.GetByQueryAsync(query));
        Assert.Equal(1, commands.Count);
    }

    [Theory]
    [InlineData(0, 20, null)]
    [InlineData(1, 0, null)]
    [InlineData(1, -1, null)]
    [InlineData(1, 11, 10)]
    [InlineData(int.MaxValue, 20, null)]
    public async Task 分页_非法页码条数和溢出在建连前拒绝(int pageIndex, int pageSize, int? maximum)
    {
        var commands = new ReadCommands();
        using var scope = new EfTestScope(builder => builder.AddInterceptors(commands));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => new BulkRepository().GetByQueryAsync(
            new RowsQuery { PageIndex = pageIndex, PageSize = pageSize, MaxPageSize = maximum }));
        Assert.Equal(0, commands.Count);
    }

    [Fact]
    public async Task 分页和流式读取_支持取消并保留租户隔离()
    {
        using var scope = new EfTestScope();
        IRepository<BulkRow, int> repository = new BulkRepository();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.GetByQueryAsync(new RowsQuery(), cancellation.Token));
        await using (var enumerator = repository.StreamAsync(row => true, cancellation.Token).GetAsyncEnumerator())
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await enumerator.MoveNextAsync());
        var rows = new List<BulkRow>();
        await foreach (var row in repository.StreamAsync(row => true)) rows.Add(row);
        Assert.Equal(1, Assert.Single(rows).TenantId);
        Assert.Empty(scope.Context.ChangeTracker.Entries());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task 游标分页_重复排序值按主键稳定翻页且不执行Count(bool descending)
    {
        var commands = new ReadCommands();
        using var scope = new EfTestScope(builder => builder.AddInterceptors(commands));
        scope.Context.Rows.AddRange(
            new BulkRow { Id = 3, TenantId = 1, Name = "third", Count = 10 },
            new BulkRow { Id = 4, TenantId = 1, Name = "fourth", Count = 20 });
        await scope.Context.SaveChangesAsync();
        scope.Context.ChangeTracker.Clear();
        commands.Count = 0;
        IRepository<BulkRow, int> repository = new BulkRepository();
        var first = await repository.GetSliceAsync(row => true, row => row.Count, pageSize: 1, descending: descending);
        Assert.True(first.HasMore);
        var last = first.Items.Single();
        Expression<Func<BulkRow, bool>> after = descending
            ? row => row.Count < last.Count || (row.Count == last.Count && row.Id < last.Id)
            : row => row.Count > last.Count || (row.Count == last.Count && row.Id > last.Id);
        var remaining = await repository.GetSliceAsync(row => true, row => row.Count, after, 2, descending);
        Assert.False(remaining.HasMore);
        var ids = first.Items.Concat(remaining.Items).Select(row => row.Id);
        Assert.Equal(descending ? new[] { 4, 3, 1 } : new[] { 1, 3, 4 }, ids);
        Assert.Equal(2, commands.Count);
        Assert.Empty(scope.Context.ChangeTracker.Entries());
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => repository.GetSliceAsync(row => true, row => row.Id, pageSize: 1001));
    }

    [Theory]
    [InlineData("Missing")]
    [InlineData("Unmapped")]
    [InlineData("TenantId")]
    [InlineData("Id")]
    [InlineData("ShadowValue")]
    public async Task 局部更新_非法字段整批拒绝且不挂接实体(string field)
    {
        using var scope = new EfTestScope();
        await Assert.ThrowsAsync<GirvsException>(() => new BulkRepository().UpdateRangeAsync(
            [new BulkRow { Id = 1, TenantId = 1, Name = "changed" }], "Name", field));
        Assert.Empty(scope.Context.ChangeTracker.Entries());
        Assert.Equal(scope.Read, scope.Context.Database.GetConnectionString());
    }

    [Fact]
    public async Task 局部更新_保留已有变更和原始并发令牌并维护更新时间()
    {
        using var scope = new EfTestScope();
        var row = await scope.Context.Rows.SingleAsync(row => row.Id == 1);
        row.Count = 99;
        row.Name = "changed";
        var originalVersion = scope.Context.Entry(row).Property(entity => entity.Version).OriginalValue;
        var before = DateTime.Now;
        await new BulkRepository().UpdateAsync(row, "Name");
        Assert.True(scope.Context.Entry(row).Property(entity => entity.Count).IsModified);
        Assert.Equal(originalVersion, scope.Context.Entry(row).Property(entity => entity.Version).OriginalValue);
        Assert.False(scope.Context.Entry(row).Property(entity => entity.Version).IsModified);
        Assert.True(row.UpdateTime >= before);
        using var unit = new UnitOfWork<BulkRow>();
        Assert.True(await unit.Commit());
        using var master = EfTestScope.CreateContext(scope.Master);
        Assert.Equal(99, (await master.Rows.SingleAsync(entity => entity.Id == 1)).Count);
    }

    [Fact]
    public async Task 局部更新_过期并发令牌拒绝覆盖()
    {
        using var scope = new EfTestScope();
        await new BulkRepository().UpdateAsync(new BulkRow { Id = 1, TenantId = 1, Name = "changed", Version = 0 }, "Name");
        using var unit = new UnitOfWork<BulkRow>();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => unit.Commit());
    }

    [Fact]
    public async Task 列表更新_后续实体非法时前面的实体不发生部分挂接()
    {
        using var scope = new EfTestScope();
        await Assert.ThrowsAsync<GirvsException>(() => new BulkRepository().UpdateRangeAsync([
            new BulkRow { Id = 1, TenantId = 1, Name = "changed" },
            new BulkRow { Id = 3, TenantId = 1, Name = null }
        ], "Name"));
        Assert.Empty(scope.Context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task 批量更新_连续调用使用当前字段和值而非缓存旧值()
    {
        using var scope = new EfTestScope();
        var repository = new BulkRepository();
        await repository.UpdateRangeAsync(row => row.Id == 1, new KeyValuePair<string, object>("Name", "one"), new("Count", 11));
        await repository.UpdateRangeAsync(row => row.Id == 1, new KeyValuePair<string, object>("Count", 22), new("Name", "two"));
        using var master = EfTestScope.CreateContext(scope.Master);
        var row = await master.Rows.SingleAsync(entity => entity.Id == 1);
        Assert.Equal("two", row.Name);
        Assert.Equal(22, row.Count);
    }

    [Fact]
    public async Task 动态筛选_空字符串和null不混淆()
    {
        using var scope = new EfTestScope();
        Assert.Equal(2, await scope.Context.Rows.WhereEqual(nameof(BulkRow.Note), null).CountAsync());
        Assert.Equal(0, await scope.Context.Rows.WhereEqual(nameof(BulkRow.Note), "").CountAsync());
    }

    [Fact]
    public async Task 跨租户更新_在挂接和修改前拒绝()
    {
        using var scope = new EfTestScope();
        await Assert.ThrowsAsync<GirvsException>(() => new BulkRepository().UpdateRangeAsync([
            new BulkRow { Id = 1, TenantId = 1, Name = "changed" },
            new BulkRow { Id = 2, TenantId = 2, Name = "blocked" }
        ], "Name"));
        Assert.Empty(scope.Context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task 未跟踪更新_未指定字段时保留原全量更新行为()
    {
        using var scope = new EfTestScope();
        await new BulkRepository().UpdateAsync(new BulkRow { Id = 1, TenantId = 1, Name = "changed", Count = 77 });
        using var unit = new UnitOfWork<BulkRow>();
        Assert.True(await unit.Commit());
        using var master = EfTestScope.CreateContext(scope.Master);
        Assert.Equal(77, (await master.Rows.SingleAsync(entity => entity.Id == 1)).Count);
    }

    [Fact]
    public async Task 已删除实体_整批校验拒绝且不挂接前面的更新()
    {
        using var scope = new EfTestScope();
        var deleted = await scope.Context.Rows.SingleAsync(row => row.Id == 1);
        scope.Context.Rows.Remove(deleted);
        var pending = new BulkRow { Id = 3, TenantId = 1, Name = "changed" };
        await Assert.ThrowsAsync<GirvsException>(() => new BulkRepository().UpdateRangeAsync([pending, deleted], "Name"));
        Assert.Equal(EntityState.Detached, scope.Context.Entry(pending).State);
        Assert.Equal(EntityState.Deleted, scope.Context.Entry(deleted).State);
    }

    [Fact]
    public async Task 租户筛选_Guid公共数据与当前租户隔离且复用编译()
    {
        var compilations = 0;
        var options = new DbContextOptionsBuilder<GuidTenantContext>().UseSqlite("Data Source=:memory:")
            .LogTo(_ => compilations++, [CoreEventId.QueryCompilationStarting]).Options;
        using var context = new GuidTenantContext(options);
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        context.Rows.AddRange(new GuidTenantRow { Id = 1, TenantId = Guid.Empty },
            new GuidTenantRow { Id = 2, TenantId = first }, new GuidTenantRow { Id = 3, TenantId = second });
        await context.SaveChangesAsync();
        var condition = new TenantCondition { ContainsPublicData = true };
        foreach (var tenant in new[] { first, second })
        {
            var rows = await context.Rows.Where(condition.ForGuidTenant(tenant)).ToListAsync();
            Assert.Equal(2, rows.Count);
            Assert.All(rows, row => Assert.True(row.TenantId == tenant || row.TenantId == Guid.Empty));
        }
        Assert.Equal(1, compilations);
    }

    [Fact]
    public async Task 动态筛选_可空值类型按实际属性类型参数化()
    {
        using var scope = new EfTestScope();
        var condition = new TenantCondition();
        Assert.Empty(await scope.Context.Rows.Where(condition.ForOptionalCount(7)).ToListAsync());
        scope.Context.Rows.Add(new BulkRow { Id = 3, TenantId = 1, Name = "third", OptionalCount = 7 });
        await scope.Context.SaveChangesAsync();
        Assert.Equal(3, (await scope.Context.Rows.Where(condition.ForOptionalCount(7)).SingleAsync()).Id);
        Assert.Empty(await scope.Context.Rows.Where(condition.ForOptionalCount(8)).ToListAsync());
    }

    [Fact]
    public void 查询缓存键_区分带总数与无总数结果()
    {
        var query = new RowsQuery();
        var counted = query.GetCacheKey();
        query.IncludeRecordCount = false;
        Assert.NotEqual(counted, query.GetCacheKey());
    }

    private sealed class ReadCommands : DbCommandInterceptor
    {
        public int Count { get; set; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Count++;
            return ValueTask.FromResult(result);
        }
    }

    private sealed class TenantCondition : GirvsRepositoryOtherQueryCondition
    {
        public Expression<Func<BulkRow, bool>> ForTenant(int tenant) => BuilderTenantBinaryExpression<BulkRow>(tenant);
        public Expression<Func<GuidTenantRow, bool>> ForGuidTenant(Guid tenant) => BuilderTenantBinaryExpression<GuidTenantRow>(tenant);
        public Expression<Func<BulkRow, bool>> ForOptionalCount(int value) => BuilderBinaryExpression<BulkRow>(
            nameof(BulkRow.OptionalCount), typeof(int?).ToString(), ExpressionType.Equal, value);
    }

    public sealed class GuidTenantContext(DbContextOptions<GuidTenantContext> options) : DbContext(options)
    {
        public DbSet<GuidTenantRow> Rows => Set<GuidTenantRow>();
    }

    public sealed class GuidTenantRow : Girvs.BusinessBasis.Entities.Entity<int>
    {
        public int Id { get; set; }
        public Guid TenantId { get; set; }
    }

    internal sealed class RowsQuery : QueryBase<BulkRow>
    {
        public override Expression<Func<BulkRow, bool>> GetQueryWhere() => row => true;
    }
}
