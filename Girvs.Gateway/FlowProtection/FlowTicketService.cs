using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Girvs.Gateway.FlowProtection;

public sealed class FlowTicketService
{
    private readonly IFlowStateStore _stateStore;
    private readonly int _lockTtlSeconds;

    public FlowTicketService(IFlowStateStore stateStore)
        : this(stateStore, lockTtlSeconds: 30)
    {
    }

    public FlowTicketService(IFlowStateStore stateStore, int lockTtlSeconds)
    {
        _stateStore = stateStore;
        _lockTtlSeconds = lockTtlSeconds;
    }

    public string CreateTicket()
    {
        return CreateRandomToken();
    }

    public string CreateAttemptId()
    {
        return CreateRandomToken();
    }

    public async Task<FlowAttemptContext> CreateFirstStepAttemptAsync(
        string businessId,
        FlowStepMatch step,
        CancellationToken cancellationToken
    )
    {
        var ticket = CreateTicket();
        var attemptId = CreateAttemptId();
        var result = await _stateStore.CreateAndReserveAsync(
            new FlowReserveRequest(
                ticket,
                step.Flow.FlowId,
                businessId,
                step.StepIndex,
                attemptId,
                step.Flow.TtlSeconds,
                _lockTtlSeconds,
                VariantKey: step.VariantKey
            ),
            cancellationToken
        );

        EnsureSuccess(result);

        return new FlowAttemptContext
        {
            Ticket = ticket,
            AttemptId = attemptId,
            BusinessId = businessId,
            Step = step,
            IsFirstStep = true,
            NextIndex = result.NextIndex,
            Completed = result.Completed,
        };
    }

    public async Task<FlowAttemptContext> ReserveNextStepAttemptAsync(
        string ticket,
        string businessId,
        FlowStepMatch step,
        CancellationToken cancellationToken
    )
    {
        var attemptId = CreateAttemptId();
        var result = await _stateStore.ReserveAsync(
            new FlowReserveRequest(
                ticket,
                step.Flow.FlowId,
                businessId,
                step.StepIndex,
                attemptId,
                step.Flow.TtlSeconds,
                _lockTtlSeconds
            ),
            cancellationToken
        );

        EnsureSuccess(result);

        return new FlowAttemptContext
        {
            Ticket = ticket,
            AttemptId = attemptId,
            BusinessId = businessId,
            Step = step,
            IsFirstStep = false,
            NextIndex = result.NextIndex,
            Completed = result.Completed,
        };
    }

    /// <summary>
    /// 按票据已锁定的流程组合推进参数化流程：候选顺位与可选前置参数一起交给状态存储原子校验，
    /// 并用返回的命中组合还原准确的 <see cref="FlowStepMatch" />（含该组合的最后一步判定）。
    /// </summary>
    public async Task<FlowAttemptContext> ReserveNextStepAttemptAsync(
        string ticket,
        string businessId,
        FlowEndpointMatch endpoint,
        string? selectorVariantKey,
        CancellationToken cancellationToken
    )
    {
        var attemptId = CreateAttemptId();
        var result = await _stateStore.ReserveAsync(
            new FlowReserveRequest(
                ticket,
                endpoint.Flow.FlowId,
                businessId,
                0,
                attemptId,
                endpoint.Flow.TtlSeconds,
                _lockTtlSeconds,
                SelectorVariantKey: selectorVariantKey ?? string.Empty,
                CandidateVariantSteps: endpoint.VariantIndexes
            ),
            cancellationToken
        );

        EnsureSuccess(result);

        if (string.IsNullOrEmpty(result.VariantKey)
            || !endpoint.TryMatchVariant(result.VariantKey, out var stepMatch)
            || result.NextIndex != stepMatch.StepIndex)
        {
            // 存储已判定成功并加锁，但返回的组合/顺位与配置不一致：尽力释放锁，避免残留至租约过期。
            await TryReleaseAsync(ticket, attemptId, cancellationToken);
            throw new FlowProtectionException(FlowProtectionErrors.StepNotAllowed);
        }

        return new FlowAttemptContext
        {
            Ticket = ticket,
            AttemptId = attemptId,
            BusinessId = businessId,
            Step = stepMatch,
            IsFirstStep = false,
            NextIndex = result.NextIndex,
            Completed = result.Completed,
        };
    }

    public async Task<FlowStoreResult> CommitAsync(
        FlowAttemptContext attempt,
        string businessIdFromResponse,
        int completedTtlSeconds,
        CancellationToken cancellationToken
    )
    {
        var result = await _stateStore.CommitAsync(
            new FlowCommitRequest(
                attempt.Ticket,
                attempt.AttemptId,
                attempt.Step.StepIndex,
                attempt.Step.IsLastStep,
                businessIdFromResponse,
                completedTtlSeconds
            ),
            cancellationToken
        );
        EnsureSuccess(result);
        attempt.NextIndex = result.NextIndex;
        attempt.Completed = result.Completed;
        attempt.IsFinalized = true;
        return result;
    }

    public async Task ReleaseAsync(FlowAttemptContext attempt, CancellationToken cancellationToken)
    {
        var result = await _stateStore.ReleaseAsync(
            new FlowReleaseRequest(attempt.Ticket, attempt.AttemptId),
            cancellationToken
        );
        EnsureSuccess(result);
        attempt.IsFinalized = true;
    }

    public async Task DeleteAsync(FlowAttemptContext attempt, CancellationToken cancellationToken)
    {
        var result = await _stateStore.DeleteAsync(
            new FlowReleaseRequest(attempt.Ticket, attempt.AttemptId),
            cancellationToken
        );
        EnsureSuccess(result);
        attempt.IsFinalized = true;
    }

    public static string GetTicketFingerprint(string ticket)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(ticket));
        return Convert.ToHexString(hash).ToLowerInvariant()[..12];
    }

    private async Task TryReleaseAsync(string ticket, string attemptId, CancellationToken cancellationToken)
    {
        try
        {
            await _stateStore.ReleaseAsync(new FlowReleaseRequest(ticket, attemptId), cancellationToken);
        }
        catch
        {
            // 释放失败不能掩盖原始错误：租约会自然到期释放。
        }
    }

    private static string CreateRandomToken()
    {
        return WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
    }

    private static void EnsureSuccess(FlowStoreResult result)
    {
        if (!result.IsSuccess)
        {
            throw new FlowProtectionException(
                FlowProtectionErrors.FromStoreError(result.ErrorCode ?? FlowErrorCode.FlowStepNotAllowed)
            );
        }
    }
}
