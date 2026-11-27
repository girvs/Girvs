using System.Net;
using Girvs.Gateway.FlowProtection;
using Girvs.Cache.CacheImps;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace Girvs.Gateway.Tests;

public class RedisFlowStateStoreTests : IClassFixture<RedisFlowStateStoreFixture>
{
    private readonly RedisFlowStateStoreFixture _fixture;
    private readonly RedisFlowStateStore _store;

    public RedisFlowStateStoreTests(RedisFlowStateStoreFixture fixture)
    {
        _fixture = fixture;
        _store = new RedisFlowStateStore(fixture.Redis);
    }

    [Fact]
    public async Task 两个同步骤Reserve_只有一个成功()
    {
        await _fixture.FlushAsync();
        await _store.CreateAndReserveAsync(CreateTicket("ticket", "start"), default);
        await _store.ReleaseAsync(new FlowReleaseRequest("ticket", "start"), default);

        var results = await Task.WhenAll(
            _store.ReserveAsync(ReserveSingleVariant("ticket", "a", 0), default),
            _store.ReserveAsync(ReserveSingleVariant("ticket", "b", 0), default)
        );

        Assert.Single(results, x => x.IsSuccess);
        Assert.Single(results, x => x.ErrorCode == FlowErrorCode.FlowInProgress);
    }

    [Fact]
    public async Task 跳过当前步骤_返回FLOW_STEP_NOT_ALLOWED()
    {
        await _fixture.FlushAsync();
        await _store.CreateAndReserveAsync(CreateTicket("ticket", "a"), default);
        await _store.CommitAsync(Commit("ticket", "a", 0, isLastStep: false), default);

        var result = await _store.ReserveAsync(ReserveSingleVariant("ticket", "b", 2), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(FlowErrorCode.FlowStepNotAllowed, result.ErrorCode);
    }

    [Fact]
    public async Task BusinessId不一致_返回FLOW_BUSINESS_MISMATCH()
    {
        await _fixture.FlushAsync();
        await _store.CreateAndReserveAsync(CreateTicket("ticket", "a"), default);
        await _store.ReleaseAsync(new FlowReleaseRequest("ticket", "a"), default);

        var result = await _store.ReserveAsync(ReserveSingleVariant("ticket", "b", 0, businessId: "biz-2"), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(FlowErrorCode.FlowBusinessMismatch, result.ErrorCode);
    }

    [Fact]
    public async Task 中间步骤Release后_可以重试相同步骤()
    {
        await _fixture.FlushAsync();
        await _store.CreateAndReserveAsync(CreateTicket("ticket", "a"), default);
        await _store.CommitAsync(Commit("ticket", "a", 0, isLastStep: false), default);
        await _store.ReserveAsync(ReserveSingleVariant("ticket", "b", 1), default);
        await _store.ReleaseAsync(new FlowReleaseRequest("ticket", "b"), default);

        var retry = await _store.ReserveAsync(ReserveSingleVariant("ticket", "c", 1), default);

        Assert.True(retry.IsSuccess);
    }

    [Theory]
    [InlineData(false, FlowErrorCode.FlowStepNotAllowed)]
    [InlineData(true, FlowErrorCode.FlowCompleted)]
    public async Task 免前置参数单组合持活跃或已完成票据重调首步_拒绝重放(bool completed, FlowErrorCode expectedError)
    {
        await _fixture.FlushAsync();
        Assert.True((await _store.CreateAndReserveAsync(CreateTicket("ticket", "a"), default)).IsSuccess);
        Assert.True((await _store.CommitAsync(Commit("ticket", "a", 0, isLastStep: false), default)).IsSuccess);
        if (completed)
        {
            Assert.True((await _store.ReserveAsync(ReserveSingleVariant("ticket", "last", 1), default)).IsSuccess);
            var commit = await _store.CommitAsync(Commit("ticket", "last", 1, isLastStep: true), default);
            Assert.True(commit.Completed);
            var ttl = await _fixture.Database.KeyTimeToLiveAsync("flow:ticket:ticket");
            Assert.NotNull(ttl);
            Assert.True(ttl <= TimeSpan.FromSeconds(60));
        }

        var replay = await _store.ReserveAsync(ReserveSingleVariant("ticket", "replay", 0), default);

        Assert.False(replay.IsSuccess);
        Assert.Equal(expectedError, replay.ErrorCode);
        var fields = await _fixture.Database.HashGetAsync("flow:ticket:ticket", ["variant", "status", "nextIndex"]);
        Assert.Equal("default", (string)fields[0]);
        Assert.Equal(completed ? "completed" : "active", (string)fields[1]);
        Assert.Equal(completed ? 2 : 1, (int)fields[2]);
    }

    [Fact]
    public async Task 锁租约过期后_允许新Attempt接管且旧Attempt不能推进()
    {
        await _fixture.FlushAsync();
        await _store.CreateAndReserveAsync(CreateTicket("ticket", "a", lockTtlSeconds: 1), default);

        await Task.Delay(TimeSpan.FromMilliseconds(1200));
        var result = await _store.ReserveAsync(ReserveSingleVariant("ticket", "b", 0), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("default", result.VariantKey);
        var staleCommit = await _store.CommitAsync(Commit("ticket", "a", 0, isLastStep: false), default);
        Assert.Equal(FlowErrorCode.FlowInProgress, staleCommit.ErrorCode);

        var currentCommit = await _store.CommitAsync(Commit("ticket", "b", 0, isLastStep: false), default);
        Assert.True(currentCommit.IsSuccess);
        Assert.Equal(1, currentCommit.NextIndex);
    }

    [Fact]
    public async Task AttemptId不匹配_Commit不推进()
    {
        await _fixture.FlushAsync();
        await _store.CreateAndReserveAsync(CreateTicket("ticket", "a"), default);

        var commit = await _store.CommitAsync(Commit("ticket", "wrong", 0, isLastStep: false), default);
        var retry = await _store.ReserveAsync(ReserveSingleVariant("ticket", "b", 0), default);

        Assert.Equal(FlowErrorCode.FlowInProgress, commit.ErrorCode);
        Assert.Equal(FlowErrorCode.FlowInProgress, retry.ErrorCode);
    }

    [Fact]
    public async Task 下游返回BusinessId_首次绑定后续严格校验()
    {
        await _fixture.FlushAsync();
        await _store.CreateAndReserveAsync(CreateTicket("ticket", "a", businessId: ""), default);
        await _store.CommitAsync(
            new FlowCommitRequest("ticket", "a", 0, false, "biz-from-service", 60),
            default
        );

        var ok = await _store.ReserveAsync(ReserveSingleVariant("ticket", "b", 1, businessId: "biz-from-service"), default);
        await _store.ReleaseAsync(new FlowReleaseRequest("ticket", "b"), default);
        var mismatch = await _store.ReserveAsync(ReserveSingleVariant("ticket", "c", 1, businessId: "other"), default);

        Assert.True(ok.IsSuccess);
        Assert.Equal(FlowErrorCode.FlowBusinessMismatch, mismatch.ErrorCode);
    }

    [Fact]
    public async Task 参数化流程建票_原子锁定组合且提交后保留()
    {
        const string key = "flow:ticket:variant-commit";
        await _fixture.FlushAsync();

        var create = await _store.CreateAndReserveAsync(
            CreateTicket("variant-commit", "start", businessId: "", variantKey: "DE"),
            default
        );

        Assert.True(create.IsSuccess);
        Assert.Equal(0, create.NextIndex);
        var fields = await _fixture.Database.HashGetAsync(key, ["variant", "status", "attemptId"]);
        Assert.Equal("DE", (string)fields[0]);
        Assert.Equal("processing", (string)fields[1]);
        Assert.Equal("start", (string)fields[2]);

        var duplicate = await _store.CreateAndReserveAsync(
            CreateTicket("variant-commit", "other", businessId: "", variantKey: "ABE"),
            default
        );
        Assert.Equal(FlowErrorCode.FlowInProgress, duplicate.ErrorCode);

        var commit = await _store.CommitAsync(
            new FlowCommitRequest("variant-commit", "start", 0, false, "biz", 60),
            default
        );
        Assert.True(commit.IsSuccess);
        Assert.False(commit.Completed);
        Assert.Equal(1, commit.NextIndex);
        Assert.Equal("DE", (string)await _fixture.Database.HashGetAsync(key, "variant"));
    }

    [Fact]
    public async Task 免前置参数单组合建票_写入非空组合键()
    {
        await _fixture.FlushAsync();

        var create = await _store.CreateAndReserveAsync(
            CreateTicket("single", "start", businessId: "biz"),
            default
        );

        Assert.True(create.IsSuccess);
        var variant = await _fixture.Database.HashGetAsync("flow:ticket:single", "variant");
        Assert.Equal("default", (string)variant);
    }

    [Fact]
    public async Task 参数化流程首步失败_删除锁定组合的票据()
    {
        const string key = "flow:ticket:variant-delete";
        await _fixture.FlushAsync();
        var create = await _store.CreateAndReserveAsync(
            CreateTicket("variant-delete", "start", businessId: "", variantKey: "ABE"),
            default
        );
        Assert.True(create.IsSuccess);
        Assert.Equal("ABE", (string)await _fixture.Database.HashGetAsync(key, "variant"));

        var delete = await _store.DeleteAsync(new FlowReleaseRequest("variant-delete", "start"), default);

        Assert.True(delete.IsSuccess);
        Assert.False(await _fixture.Database.KeyExistsAsync(key));
    }

    [Fact]
    public async Task 参数化持票推进_单次Lua原子命中组合与顺位()
    {
        await _fixture.FlushAsync();
        await _store.CreateAndReserveAsync(
            CreateTicket("param", "start", variantKey: "ABE"),
            default
        );
        await _store.CommitAsync(new FlowCommitRequest("param", "start", 0, false, "", 60), default);

        var result = await _store.ReserveAsync(
            ReserveVariant("param", "b1", 1, null, [new FlowVariantStepIndex("ABE", 1)]),
            default
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.NextIndex);
        Assert.Equal("ABE", result.VariantKey);
        Assert.Equal("processing", (string)await _fixture.Database.HashGetAsync("flow:ticket:param", "status"));
    }

    [Fact]
    public async Task 参数化持票推进_前置参数一致放行冲突返回FLOW_SELECTOR_MISMATCH()
    {
        await _fixture.FlushAsync();
        await _store.CreateAndReserveAsync(
            CreateTicket("param", "start", variantKey: "ABE"),
            default
        );
        await _store.CommitAsync(new FlowCommitRequest("param", "start", 0, false, "", 60), default);

        var matched = await _store.ReserveAsync(
            ReserveVariant("param", "ok", 1, "ABE", [new FlowVariantStepIndex("ABE", 1)]),
            default
        );
        Assert.True(matched.IsSuccess);
        await _store.ReleaseAsync(new FlowReleaseRequest("param", "ok"), default);

        var mismatch = await _store.ReserveAsync(
            ReserveVariant("param", "conflict", 1, "ADE", [new FlowVariantStepIndex("ABE", 1)]),
            default
        );

        Assert.False(mismatch.IsSuccess);
        Assert.Equal(FlowErrorCode.FlowSelectorMismatch, mismatch.ErrorCode);
    }

    [Fact]
    public async Task 参数化重叠接口D_按锁定组合命中各自顺位()
    {
        await _fixture.FlushAsync();
        await _store.CreateAndReserveAsync(
            CreateTicket("ade", "a0", variantKey: "ADE"),
            default
        );
        await _store.CommitAsync(new FlowCommitRequest("ade", "a0", 0, false, "", 60), default);
        await _store.CreateAndReserveAsync(
            CreateTicket("de", "d0", variantKey: "DE"),
            default
        );
        await _store.ReleaseAsync(new FlowReleaseRequest("de", "d0"), default);

        FlowVariantStepIndex[] dCandidates = [new("ADE", 1), new("DE", 0)];
        var ade = await _store.ReserveAsync(ReserveVariant("ade", "a1", 99, null, dCandidates), default);
        var de = await _store.ReserveAsync(ReserveVariant("de", "d1", 99, null, dCandidates), default);

        Assert.True(ade.IsSuccess);
        Assert.Equal("ADE", ade.VariantKey);
        Assert.Equal(1, ade.NextIndex);
        Assert.True(de.IsSuccess);
        Assert.Equal("DE", de.VariantKey);
        Assert.Equal(0, de.NextIndex);
    }

    [Fact]
    public async Task 参数化持票推进_跨组合跳步与重调首步均返回FLOW_STEP_NOT_ALLOWED()
    {
        await _fixture.FlushAsync();
        await _store.CreateAndReserveAsync(
            CreateTicket("param", "start", variantKey: "ABE"),
            default
        );
        await _store.CommitAsync(new FlowCommitRequest("param", "start", 0, false, "", 60), default);

        var crossVariant = await _store.ReserveAsync(
            ReserveVariant("param", "cross", 1, null, [new FlowVariantStepIndex("ADE", 1), new FlowVariantStepIndex("DE", 0)]),
            default
        );
        var skipStep = await _store.ReserveAsync(
            ReserveVariant("param", "skip", 2, null, [new FlowVariantStepIndex("ABE", 2)]),
            default
        );
        var restart = await _store.ReserveAsync(
            ReserveVariant("param", "restart", 0, null, [new FlowVariantStepIndex("ABE", 0)]),
            default
        );

        Assert.Equal(FlowErrorCode.FlowStepNotAllowed, crossVariant.ErrorCode);
        Assert.Equal(FlowErrorCode.FlowStepNotAllowed, skipStep.ErrorCode);
        Assert.Equal(FlowErrorCode.FlowStepNotAllowed, restart.ErrorCode);
    }

    [Fact]
    public async Task 参数化持票推进_业务标识不匹配返回FLOW_BUSINESS_MISMATCH()
    {
        await _fixture.FlushAsync();
        await _store.CreateAndReserveAsync(
            CreateTicket("param", "start", variantKey: "ABE"),
            default
        );
        await _store.CommitAsync(new FlowCommitRequest("param", "start", 0, false, "", 60), default);

        var result = await _store.ReserveAsync(
            ReserveVariant("param", "other", 1, null, [new FlowVariantStepIndex("ABE", 1)], businessId: "biz-2"),
            default
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(FlowErrorCode.FlowBusinessMismatch, result.ErrorCode);
    }

    [Fact]
    public async Task 参数化持票推进_并发请求仅一次获得步骤锁()
    {
        await _fixture.FlushAsync();
        await _store.CreateAndReserveAsync(
            CreateTicket("param", "start", variantKey: "ABE"),
            default
        );
        await _store.CommitAsync(new FlowCommitRequest("param", "start", 0, false, "", 60), default);

        FlowVariantStepIndex[] candidates = [new("ABE", 1)];
        var results = await Task.WhenAll(
            _store.ReserveAsync(ReserveVariant("param", "first", 1, null, candidates), default),
            _store.ReserveAsync(ReserveVariant("param", "second", 1, null, candidates), default)
        );

        Assert.Single(results, result => result.IsSuccess);
        Assert.Single(results, result => result.ErrorCode == FlowErrorCode.FlowInProgress);
    }

    [Fact]
    public async Task 参数化持票推进_中间步Release保留组合并可重试()
    {
        const string key = "flow:ticket:param";
        await _fixture.FlushAsync();
        await _store.CreateAndReserveAsync(
            CreateTicket("param", "start", variantKey: "ABE"),
            default
        );
        await _store.CommitAsync(new FlowCommitRequest("param", "start", 0, false, "", 60), default);

        FlowVariantStepIndex[] candidates = [new("ABE", 1)];
        await _store.ReserveAsync(ReserveVariant("param", "failed", 1, null, candidates), default);
        var release = await _store.ReleaseAsync(new FlowReleaseRequest("param", "failed"), default);
        var retry = await _store.ReserveAsync(ReserveVariant("param", "retry", 1, null, candidates), default);

        Assert.True(release.IsSuccess);
        Assert.True(retry.IsSuccess);
        Assert.Equal("ABE", retry.VariantKey);
        Assert.Equal(1, retry.NextIndex);
        Assert.Equal("ABE", (string)await _fixture.Database.HashGetAsync(key, "variant"));
    }

    [Fact]
    public async Task 参数化持票推进_终步完成缩短Ttl并拒绝重放()
    {
        await _fixture.FlushAsync();
        await _store.CreateAndReserveAsync(
            CreateTicket("param", "start", variantKey: "ABE"),
            default
        );
        await _store.CommitAsync(new FlowCommitRequest("param", "start", 0, false, "", 60), default);
        await _store.ReserveAsync(
            ReserveVariant("param", "middle", 1, null, [new FlowVariantStepIndex("ABE", 1)]),
            default
        );
        await _store.CommitAsync(new FlowCommitRequest("param", "middle", 1, false, "", 60), default);
        await _store.ReserveAsync(
            ReserveVariant("param", "last", 2, null, [new FlowVariantStepIndex("ABE", 2)]),
            default
        );

        var commit = await _store.CommitAsync(new FlowCommitRequest("param", "last", 2, true, "", 60), default);
        var replay = await _store.ReserveAsync(
            ReserveVariant("param", "replay", 2, null, [new FlowVariantStepIndex("ABE", 2)]),
            default
        );
        var ttl = await _fixture.Database.KeyTimeToLiveAsync("flow:ticket:param");

        Assert.True(commit.IsSuccess);
        Assert.True(commit.Completed);
        Assert.Equal(FlowErrorCode.FlowCompleted, replay.ErrorCode);
        Assert.NotNull(ttl);
        Assert.True(ttl <= TimeSpan.FromSeconds(60));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("ABE")]
    public async Task 旧票据的锁定组合键为空_拒绝推进且不加锁(string? selectorVariantKey)
    {
        const string key = "flow:ticket:legacy";
        await _fixture.FlushAsync();
        await _fixture.Database.HashSetAsync(
            key,
            [
                new HashEntry("flowId", "flow-1"),
                new HashEntry("businessId", "biz-1"),
                new HashEntry("nextIndex", 1),
                new HashEntry("status", "active"),
                new HashEntry("variant", string.Empty),
            ]
        );

        var result = await _store.ReserveAsync(
            ReserveVariant("legacy", "next", 1, selectorVariantKey, [new FlowVariantStepIndex("ABE", 1)]),
            default
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(FlowErrorCode.FlowStepNotAllowed, result.ErrorCode);
        Assert.Equal("active", (string)await _fixture.Database.HashGetAsync(key, "status"));
        Assert.False(await _fixture.Database.HashExistsAsync(key, "attemptId"));
    }

    [Fact]
    public async Task 参数化持票推进_锁未过期时前置参数冲突优先返回FLOW_SELECTOR_MISMATCH()
    {
        await _fixture.FlushAsync();
        await _store.CreateAndReserveAsync(
            CreateTicket("param", "start", variantKey: "ABE"),
            default
        );
        await _store.CommitAsync(new FlowCommitRequest("param", "start", 0, false, "", 60), default);

        var locked = await _store.ReserveAsync(
            ReserveVariant("param", "holder", 1, null, [new FlowVariantStepIndex("ABE", 1)]),
            default
        );
        Assert.True(locked.IsSuccess);

        var conflict = await _store.ReserveAsync(
            ReserveVariant("param", "other", 1, "ADE", [new FlowVariantStepIndex("ABE", 1)]),
            default
        );

        Assert.Equal(FlowErrorCode.FlowSelectorMismatch, conflict.ErrorCode);
    }

    private static FlowReserveRequest ReserveVariant(
        string ticket,
        string attempt,
        int expectedIndex,
        string? selectorVariantKey,
        IReadOnlyList<FlowVariantStepIndex> candidates,
        string businessId = "biz-1",
        int lockTtlSeconds = 30
    )
    {
        return new FlowReserveRequest(
            ticket,
            "flow-1",
            businessId,
            expectedIndex,
            attempt,
            300,
            lockTtlSeconds,
            VariantKey: string.Empty,
            SelectorVariantKey: selectorVariantKey ?? string.Empty,
            CandidateVariantSteps: candidates
        );
    }

    private static FlowReserveRequest ReserveSingleVariant(
        string ticket,
        string attempt,
        int index,
        string businessId = "biz-1"
    ) => ReserveVariant(ticket, attempt, 0, selectorVariantKey: null, [new FlowVariantStepIndex("default", index)], businessId);

    private static FlowReserveRequest CreateTicket(
        string ticket,
        string attempt,
        string businessId = "biz-1",
        int lockTtlSeconds = 30,
        string variantKey = "default"
    )
    {
        return new FlowReserveRequest(
            ticket, "flow-1", businessId, 0, attempt, 300, lockTtlSeconds,
            VariantKey: variantKey, SelectorVariantKey: string.Empty, CandidateVariantSteps: null
        );
    }

    private static FlowCommitRequest Commit(string ticket, string attempt, int index, bool isLastStep)
    {
        return new FlowCommitRequest(ticket, attempt, index, isLastStep, "", 60);
    }
}

public sealed class RedisFlowStateStoreFixture : IAsyncLifetime
{
    private readonly RedisContainer _container = new RedisBuilder("redis:7-alpine")
        .Build();

    private ConnectionMultiplexer _connection;

    public IRedisConnectionWrapper Redis { get; private set; }

    public IDatabase Database => _connection.GetDatabase();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _connection = await ConnectionMultiplexer.ConnectAsync($"{_container.GetConnectionString()},allowAdmin=true");
        Redis = new TestRedisConnectionWrapper(_connection);
    }

    public async Task DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        await _container.DisposeAsync();
    }

    public async Task FlushAsync()
    {
        foreach (var endPoint in _connection.GetEndPoints())
        {
            await _connection.GetServer(endPoint).FlushDatabaseAsync();
        }
    }

    private sealed class TestRedisConnectionWrapper(ConnectionMultiplexer connection) : IRedisConnectionWrapper
    {
        public string Instance => string.Empty;

        public Task<IDatabase> GetDatabaseAsync()
        {
            return Task.FromResult(connection.GetDatabase());
        }

        public IDatabase GetDatabase()
        {
            return connection.GetDatabase();
        }

        public Task<IServer> GetServerAsync(EndPoint endPoint)
        {
            return Task.FromResult(connection.GetServer(endPoint));
        }

        public Task<EndPoint[]> GetEndPointsAsync()
        {
            return Task.FromResult(connection.GetEndPoints());
        }

        public Task<ISubscriber> GetSubscriberAsync()
        {
            return Task.FromResult(connection.GetSubscriber());
        }

        public ISubscriber GetSubscriber()
        {
            return connection.GetSubscriber();
        }

        public async Task FlushDatabaseAsync()
        {
            foreach (var endPoint in connection.GetEndPoints())
            {
                await connection.GetServer(endPoint).FlushDatabaseAsync();
            }
        }
    }
}
