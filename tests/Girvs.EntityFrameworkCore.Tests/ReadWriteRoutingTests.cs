using Girvs;
using Girvs.EntityFrameworkCore.DbContextExtensions;
using Girvs.EntityFrameworkCore.Enumerations;
using Girvs.EntityFrameworkCore.UoW;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Girvs.EntityFrameworkCore.Tests;

[Collection(EfEngineCollection.Name)]
public class ReadWriteRoutingTests
{
    [Fact]
    public async Task 批量删除_未创建工作单元也写主库并保留租户过滤()
    {
        using var scope = new EfTestScope();
        await new BulkRepository().DeleteRangeAsync(row => true);
        using var master = EfTestScope.CreateContext(scope.Master);
        Assert.Equal(2, (await master.Rows.SingleAsync()).TenantId);
        using var read = EfTestScope.CreateContext(scope.Read);
        Assert.Equal(2, await read.Rows.CountAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task 显式写事务_批量操作和跟踪保存共同提交或回滚(bool commit)
    {
        using var scope = new EfTestScope();
        var repository = new BulkRepository();
        await using (var transaction = await scope.Context.BeginWriteTransactionAsync())
        {
            await repository.UpdateRangeAsync(row => row.Id == 1, new KeyValuePair<string, object>("Name", "updated"));
            scope.Context.Rows.Add(new BulkRow { Id = 3, TenantId = 1, Name = "third" });
            using var unit = new UnitOfWork<BulkRow>();
            Assert.True(await unit.Commit());
            // Commit 仅保存跟踪变更，事务仍由调用方拥有。
            Assert.Same(transaction, scope.Context.Database.CurrentTransaction);
            if (commit) await transaction.CommitAsync();
            else await transaction.RollbackAsync();
        }
        using var master = EfTestScope.CreateContext(scope.Master);
        Assert.Equal(commit ? "updated" : "first", (await master.Rows.SingleAsync(row => row.Id == 1)).Name);
        Assert.Equal(commit ? 3 : 2, await master.Rows.CountAsync());
    }

    [Fact]
    public async Task 主库事务_重复选择主库允许_跨库切换拒绝()
    {
        using var scope = new EfTestScope();
        await using var transaction = await scope.Context.BeginWriteTransactionAsync();
        scope.Context.SwitchReadWriteDataBase(DataBaseWriteAndRead.Write);
        Assert.Throws<GirvsException>(() => scope.Context.SwitchReadWriteDataBase(DataBaseWriteAndRead.Read));
        Assert.Same(transaction, scope.Context.Database.CurrentTransaction);
    }

    [Fact]
    public async Task 读库事务_批量写拒绝跨库且不影响读库()
    {
        using var scope = new EfTestScope();
        await using var transaction = await scope.Context.Database.BeginTransactionAsync();
        await Assert.ThrowsAsync<GirvsException>(() => new BulkRepository().DeleteRangeAsync(row => true));
        Assert.Equal(2, await scope.Context.Rows.CountAsync());
    }

    [Fact]
    public async Task 手动打开读连接_拒绝跨库切换且保留连接状态()
    {
        using var scope = new EfTestScope();
        await scope.Context.Database.OpenConnectionAsync();
        Assert.Throws<GirvsException>(() => scope.Context.SwitchReadWriteDataBase(DataBaseWriteAndRead.Write));
        Assert.Equal(System.Data.ConnectionState.Open, scope.Context.Database.GetDbConnection().State);
        await scope.Context.Database.CloseConnectionAsync();
    }

    [Fact]
    public async Task 批量更新后无跟踪变更_Commit返回false且更新已经生效()
    {
        using var scope = new EfTestScope();
        await new BulkRepository().UpdateRangeAsync(row => row.Id == 1, new KeyValuePair<string, object>("Name", "updated"));
        using var unit = new UnitOfWork<BulkRow>();
        Assert.False(await unit.Commit());
        using var master = EfTestScope.CreateContext(scope.Master);
        Assert.Equal("updated", (await master.Rows.SingleAsync(row => row.Id == 1)).Name);
    }
}
