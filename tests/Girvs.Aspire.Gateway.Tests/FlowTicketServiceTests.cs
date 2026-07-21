using System.Reflection;
using Girvs.Aspire.Gateway.FlowProtection;
using Girvs.Aspire.Gateway.FlowProtection.Configuration;

namespace Girvs.Aspire.Gateway.Tests;

public class FlowTicketServiceTests
{
    [Fact]
    public void CreateTicket_生成43字符Base64Url且每次不同()
    {
        var service = CreateService();

        var first = service.CreateTicket();
        var second = service.CreateTicket();

        Assert.Matches("^[A-Za-z0-9_-]{43}$", first);
        Assert.Matches("^[A-Za-z0-9_-]{43}$", second);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void CreateAttemptId_生成43字符Base64Url且每次不同()
    {
        var service = CreateService();

        var first = service.CreateAttemptId();
        var second = service.CreateAttemptId();

        Assert.Matches("^[A-Za-z0-9_-]{43}$", first);
        Assert.Matches("^[A-Za-z0-9_-]{43}$", second);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void GetTicketFingerprint_只返回Sha256前12位()
    {
        var fingerprint = FlowTicketService.GetTicketFingerprint("ticket-value");

        Assert.Matches("^[a-f0-9]{12}$", fingerprint);
    }

    [Fact]
    public async Task Reserve跳步_映射为FLOW_STEP_NOT_ALLOWED()
    {
        var service = CreateService(new FakeFlowStateStore(FlowStoreResult.Failed(FlowErrorCode.FlowStepNotAllowed)));

        var error = await Assert.ThrowsAsync<FlowProtectionException>(
            () => service.ReserveNextStepAttemptAsync("ticket", "biz", TestFlows.Match(2), default)
        );

        Assert.Equal("FLOW_STEP_NOT_ALLOWED", error.Code);
    }

    [Fact]
    public async Task 第一步_创建Ticket并Reserve成功()
    {
        var store = new FakeFlowStateStore(FlowStoreResult.Success(1, completed: false));
        var service = CreateService(store);

        var attempt = await service.CreateFirstStepAttemptAsync("biz", TestFlows.Match(0), default);

        Assert.Matches("^[A-Za-z0-9_-]{43}$", attempt.Ticket);
        Assert.Matches("^[A-Za-z0-9_-]{43}$", attempt.AttemptId);
        Assert.True(attempt.IsFirstStep);
        Assert.Equal("biz", store.LastReserveRequest?.BusinessId);
        Assert.Equal(0, store.LastReserveRequest?.ExpectedIndex);
    }

    [Fact]
    public async Task 后续步骤_Reserve成功返回Attempt上下文()
    {
        var service = CreateService(new FakeFlowStateStore(FlowStoreResult.Success(2, completed: false)));

        var attempt = await service.ReserveNextStepAttemptAsync("ticket", "biz", TestFlows.Match(1), default);

        Assert.Equal("ticket", attempt.Ticket);
        Assert.False(attempt.IsFirstStep);
        Assert.Equal(1, attempt.Step.StepIndex);
    }

    [Fact]
    public void 状态请求模型_不包含主体和租户字段()
    {
        var names = typeof(FlowReserveRequest)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(x => x.Name)
            .ToArray();

        Assert.DoesNotContain("UserId", names);
        Assert.DoesNotContain("TenantId", names);
    }

    private static FlowTicketService CreateService()
    {
        return CreateService(new FakeFlowStateStore(FlowStoreResult.Success(1, completed: false)));
    }

    private static FlowTicketService CreateService(IFlowStateStore store)
    {
        return new FlowTicketService(store);
    }

    private sealed class FakeFlowStateStore(FlowStoreResult result) : IFlowStateStore
    {
        public FlowReserveRequest LastReserveRequest { get; private set; }

        public Task<FlowStoreResult> CreateAndReserveAsync(
            FlowReserveRequest request,
            CancellationToken cancellationToken
        )
        {
            LastReserveRequest = request;
            return Task.FromResult(result);
        }

        public Task<FlowStoreResult> ReserveAsync(FlowReserveRequest request, CancellationToken cancellationToken)
        {
            LastReserveRequest = request;
            return Task.FromResult(result);
        }

        public Task<FlowStoreResult> CommitAsync(FlowCommitRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(result);
        }

        public Task<FlowStoreResult> ReleaseAsync(FlowReleaseRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(result);
        }

        public Task<FlowStoreResult> DeleteAsync(FlowReleaseRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(result);
        }
    }

    private static class TestFlows
    {
        public static FlowStepMatch Match(int stepIndex)
        {
            var steps = new[]
            {
                new FlowStepDefinition("POST", "/api/user1"),
                new FlowStepDefinition("POST", "/api/role1"),
                new FlowStepDefinition("POST", "/api/abc1"),
            };
            var flow = new FlowDefinition("f1", 300, steps);

            return new FlowStepMatch(flow, steps[stepIndex], stepIndex);
        }
    }
}
