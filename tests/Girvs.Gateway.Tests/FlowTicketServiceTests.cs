using System.Reflection;
using Girvs.Gateway.FlowProtection;
using Girvs.Gateway.FlowProtection.Configuration;
using static Girvs.Gateway.Tests.FlowDefinitionRegistryVariantsTests;

namespace Girvs.Gateway.Tests;

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
            () => service.ReserveNextStepAttemptAsync("ticket", "biz", SingleVariantEndpoint("/api/abc1"), null, default)
        );

        Assert.Equal("FLOW_STEP_NOT_ALLOWED", error.Code);
    }

    [Fact]
    public async Task 第一步_创建Ticket并Reserve成功()
    {
        var store = new FakeFlowStateStore(FlowStoreResult.Success(1, false, DefaultVariantKey));
        var service = CreateService(store);
        var endpoint = SingleVariantEndpoint("/api/user1");
        Assert.True(endpoint.TryMatchVariant(DefaultVariantKey, out var step));

        var attempt = await service.CreateFirstStepAttemptAsync("biz", step, default);

        Assert.Matches("^[A-Za-z0-9_-]{43}$", attempt.Ticket);
        Assert.Matches("^[A-Za-z0-9_-]{43}$", attempt.AttemptId);
        Assert.True(attempt.IsFirstStep);
        Assert.Equal("biz", store.LastReserveRequest?.BusinessId);
        Assert.Equal(0, store.LastReserveRequest?.ExpectedIndex);
        Assert.Equal(DefaultVariantKey, store.LastReserveRequest?.VariantKey);
    }

    [Fact]
    public async Task 后续步骤_Reserve成功返回Attempt上下文()
    {
        var service = CreateService(new FakeFlowStateStore(FlowStoreResult.Success(1, false, DefaultVariantKey)));

        var attempt = await service.ReserveNextStepAttemptAsync("ticket", "biz", SingleVariantEndpoint("/api/role1"), null, default);

        Assert.Equal("ticket", attempt.Ticket);
        Assert.False(attempt.IsFirstStep);
        Assert.Equal(1, attempt.Step.StepIndex);
        Assert.Equal(DefaultVariantKey, attempt.Step.VariantKey);
    }

    [Fact]
    public async Task 参数化持票推进_按命中组合还原步骤与终步标记()
    {
        var store = new FakeFlowStateStore(FlowStoreResult.Success(1, completed: false, variantKey: "DE"));
        var service = CreateService(store);

        var attempt = await service.ReserveNextStepAttemptAsync("ticket", "biz", VariantEndpoint("/e"), null, default);

        Assert.Equal("ticket", attempt.Ticket);
        Assert.False(attempt.IsFirstStep);
        Assert.Equal("DE", attempt.Step.VariantKey);
        Assert.Equal(1, attempt.Step.StepIndex);
        Assert.True(attempt.Step.IsLastStep);
        Assert.Equal(1, attempt.NextIndex);
        Assert.Equal(string.Empty, store.LastReserveRequest?.SelectorVariantKey);
        var candidates = Assert.IsAssignableFrom<IReadOnlyList<FlowVariantStepIndex>>(
            store.LastReserveRequest?.CandidateVariantSteps
        );
        Assert.Contains(candidates, candidate => candidate.VariantKey == "DE" && candidate.StepIndex == 1);
    }

    [Fact]
    public async Task 参数化持票推进_把可选前置参数透传给状态存储()
    {
        var store = new FakeFlowStateStore(FlowStoreResult.Success(2, completed: false, variantKey: "ABE"));
        var service = CreateService(store);

        var attempt = await service.ReserveNextStepAttemptAsync("ticket", "biz", VariantEndpoint("/e"), " abe ", default);

        Assert.Equal(" abe ", store.LastReserveRequest?.SelectorVariantKey);
        Assert.Equal("ABE", attempt.Step.VariantKey);
        Assert.Equal(2, attempt.Step.StepIndex);
        Assert.True(attempt.Step.IsLastStep);
    }

    [Theory]
    [InlineData(null, 1)]
    [InlineData("XYZ", 1)]
    [InlineData("ABE", 1)]
    public async Task 参数化持票推进_命中组合缺失或顺位不符_抛StepNotAllowed(string? variantKey, int nextIndex)
    {
        var service = CreateService(
            new FakeFlowStateStore(FlowStoreResult.Success(nextIndex, completed: false, variantKey: variantKey))
        );

        var error = await Assert.ThrowsAsync<FlowProtectionException>(
            () => service.ReserveNextStepAttemptAsync("ticket", "biz", VariantEndpoint("/e"), null, default)
        );

        Assert.Equal("FLOW_STEP_NOT_ALLOWED", error.Code);
    }

    [Theory]
    [InlineData(null, 1)]
    [InlineData("XYZ", 1)]
    [InlineData("ABE", 1)]
    public async Task 参数化持票推进_命中组合缺失或顺位不符_抛错前尽力释放租约(string? variantKey, int nextIndex)
    {
        var store = new FakeFlowStateStore(
            FlowStoreResult.Success(nextIndex, completed: false, variantKey: variantKey)
        );
        var service = CreateService(store);

        await Assert.ThrowsAsync<FlowProtectionException>(
            () => service.ReserveNextStepAttemptAsync("ticket", "biz", VariantEndpoint("/e"), null, default)
        );

        Assert.Equal(1, store.ReleaseCount);
        Assert.Equal("ticket", store.LastReleaseRequest?.Ticket);
        Assert.Equal(store.LastReserveRequest?.AttemptId, store.LastReleaseRequest?.AttemptId);
    }

    [Fact]
    public void FlowStoreResult_成功保留组合键且失败保留错误码()
    {
        var variantSuccess = FlowStoreResult.Success(4, false, "ABE");
        Assert.True(variantSuccess.IsSuccess);
        Assert.Null(variantSuccess.ErrorCode);
        Assert.Equal(4, variantSuccess.NextIndex);
        Assert.False(variantSuccess.Completed);
        Assert.Equal("ABE", variantSuccess.VariantKey);

        var failed = FlowStoreResult.Failed(FlowErrorCode.FlowStepNotAllowed);
        Assert.False(failed.IsSuccess);
        Assert.Equal(FlowErrorCode.FlowStepNotAllowed, failed.ErrorCode);
        Assert.Null(failed.NextIndex);
        Assert.False(failed.Completed);
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
        return CreateService(new FakeFlowStateStore(FlowStoreResult.Success(1, false, DefaultVariantKey)));
    }

    private static FlowTicketService CreateService(IFlowStateStore store)
    {
        return new FlowTicketService(store);
    }

    private sealed class FakeFlowStateStore(FlowStoreResult result) : IFlowStateStore
    {
        public FlowReserveRequest LastReserveRequest { get; private set; }

        public FlowReleaseRequest LastReleaseRequest { get; private set; }

        public int ReleaseCount { get; private set; }

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
            LastReleaseRequest = request;
            ReleaseCount++;
            return Task.FromResult(result);
        }

        public Task<FlowStoreResult> DeleteAsync(FlowReleaseRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(result);
        }
    }

    private static FlowEndpointMatch VariantEndpoint(string path)
    {
        var registry = new FlowDefinitionRegistry(FlowDefinitionRegistryVariantsTests.CreateOptions());
        Assert.True(registry.TryGetEndpoint("POST", path, out var endpoint));
        return endpoint;
    }

    private static FlowEndpointMatch SingleVariantEndpoint(string path)
    {
        var registry = new FlowDefinitionRegistry(new FlowProtectionOptions
        {
            Flows = [CreateSingleVariant("f1", "/api/user1", "/api/role1", "/api/abc1")],
        });
        Assert.True(registry.TryGetEndpoint("POST", path, out var endpoint));
        return endpoint;
    }
}
