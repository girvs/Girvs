using System.Net;
using Girvs.Aspire.Gateway.FlowProtection;
using Girvs.Cache.CacheImps;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace Girvs.Aspire.Gateway.Tests;

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
        await _store.CreateAndReserveAsync(Reserve("ticket", "start", 0), default);
        await _store.ReleaseAsync(new FlowReleaseRequest("ticket", "start"), default);

        var results = await Task.WhenAll(
            _store.ReserveAsync(Reserve("ticket", "a", 0), default),
            _store.ReserveAsync(Reserve("ticket", "b", 0), default)
        );

        Assert.Single(results, x => x.IsSuccess);
        Assert.Single(results, x => x.ErrorCode == FlowErrorCode.FlowInProgress);
    }

    [Fact]
    public async Task 跳过当前步骤_返回FLOW_STEP_NOT_ALLOWED()
    {
        await _fixture.FlushAsync();
        await _store.CreateAndReserveAsync(Reserve("ticket", "a", 0), default);
        await _store.CommitAsync(Commit("ticket", "a", 0, isLastStep: false), default);

        var result = await _store.ReserveAsync(Reserve("ticket", "b", 2), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(FlowErrorCode.FlowStepNotAllowed, result.ErrorCode);
    }

    [Fact]
    public async Task BusinessId不一致_返回FLOW_BUSINESS_MISMATCH()
    {
        await _fixture.FlushAsync();
        await _store.CreateAndReserveAsync(Reserve("ticket", "a", 0, businessId: "biz-1"), default);
        await _store.ReleaseAsync(new FlowReleaseRequest("ticket", "a"), default);

        var result = await _store.ReserveAsync(Reserve("ticket", "b", 0, businessId: "biz-2"), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(FlowErrorCode.FlowBusinessMismatch, result.ErrorCode);
    }

    [Fact]
    public async Task 中间步骤Release后_可以重试相同步骤()
    {
        await _fixture.FlushAsync();
        await _store.CreateAndReserveAsync(Reserve("ticket", "a", 0), default);
        await _store.CommitAsync(Commit("ticket", "a", 0, isLastStep: false), default);
        await _store.ReserveAsync(Reserve("ticket", "b", 1), default);
        await _store.ReleaseAsync(new FlowReleaseRequest("ticket", "b"), default);

        var retry = await _store.ReserveAsync(Reserve("ticket", "c", 1), default);

        Assert.True(retry.IsSuccess);
    }

    [Fact]
    public async Task 完成后再次Reserve_返回FLOW_COMPLETED并缩短Ttl()
    {
        await _fixture.FlushAsync();
        await _store.CreateAndReserveAsync(Reserve("ticket", "a", 0), default);

        var commit = await _store.CommitAsync(Commit("ticket", "a", 0, isLastStep: true), default);
        var replay = await _store.ReserveAsync(Reserve("ticket", "b", 0), default);
        var ttl = await _fixture.Database.KeyTimeToLiveAsync("flow:ticket:ticket");

        Assert.True(commit.Completed);
        Assert.Equal(FlowErrorCode.FlowCompleted, replay.ErrorCode);
        Assert.True(ttl <= TimeSpan.FromSeconds(60));
    }

    [Fact]
    public async Task 锁租约过期后_允许新Attempt接管()
    {
        await _fixture.FlushAsync();
        await _store.CreateAndReserveAsync(Reserve("ticket", "a", 0, lockTtlSeconds: 1), default);

        await Task.Delay(TimeSpan.FromMilliseconds(1200));
        var result = await _store.ReserveAsync(Reserve("ticket", "b", 0), default);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task AttemptId不匹配_Commit不推进()
    {
        await _fixture.FlushAsync();
        await _store.CreateAndReserveAsync(Reserve("ticket", "a", 0), default);

        var commit = await _store.CommitAsync(Commit("ticket", "wrong", 0, isLastStep: false), default);
        var retry = await _store.ReserveAsync(Reserve("ticket", "b", 0), default);

        Assert.Equal(FlowErrorCode.FlowInProgress, commit.ErrorCode);
        Assert.Equal(FlowErrorCode.FlowInProgress, retry.ErrorCode);
    }

    [Fact]
    public async Task 下游返回BusinessId_首次绑定后续严格校验()
    {
        await _fixture.FlushAsync();
        await _store.CreateAndReserveAsync(Reserve("ticket", "a", 0, businessId: ""), default);
        await _store.CommitAsync(
            new FlowCommitRequest("ticket", "a", 0, false, "biz-from-service", 60),
            default
        );

        var ok = await _store.ReserveAsync(Reserve("ticket", "b", 1, businessId: "biz-from-service"), default);
        await _store.ReleaseAsync(new FlowReleaseRequest("ticket", "b"), default);
        var mismatch = await _store.ReserveAsync(Reserve("ticket", "c", 1, businessId: "other"), default);

        Assert.True(ok.IsSuccess);
        Assert.Equal(FlowErrorCode.FlowBusinessMismatch, mismatch.ErrorCode);
    }

    private static FlowReserveRequest Reserve(
        string ticket,
        string attempt,
        int index,
        string businessId = "biz-1",
        int lockTtlSeconds = 30
    )
    {
        return new FlowReserveRequest(ticket, "flow-1", businessId, index, attempt, 300, lockTtlSeconds);
    }

    private static FlowCommitRequest Commit(string ticket, string attempt, int index, bool isLastStep)
    {
        return new FlowCommitRequest(ticket, attempt, index, isLastStep, "", 60);
    }
}

public sealed class RedisFlowStateStoreFixture : IAsyncLifetime
{
    private readonly RedisContainer _container = new RedisBuilder()
        .WithImage("redis:7-alpine")
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
