using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Girvs.Aspire.Gateway.FlowProtection;

public sealed class FlowTicketService(IFlowStateStore stateStore)
{
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
        var result = await stateStore.CreateAndReserveAsync(
            new FlowReserveRequest(
                ticket,
                step.Flow.FlowId,
                businessId,
                step.StepIndex,
                attemptId,
                step.Flow.TtlSeconds,
                LockTtlSeconds: 30
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
        var result = await stateStore.ReserveAsync(
            new FlowReserveRequest(
                ticket,
                step.Flow.FlowId,
                businessId,
                step.StepIndex,
                attemptId,
                step.Flow.TtlSeconds,
                LockTtlSeconds: 30
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

    public async Task<FlowStoreResult> CommitAsync(
        FlowAttemptContext attempt,
        string businessIdFromResponse,
        int completedTtlSeconds,
        CancellationToken cancellationToken
    )
    {
        var result = await stateStore.CommitAsync(
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
        var result = await stateStore.ReleaseAsync(
            new FlowReleaseRequest(attempt.Ticket, attempt.AttemptId),
            cancellationToken
        );
        EnsureSuccess(result);
        attempt.IsFinalized = true;
    }

    public async Task DeleteAsync(FlowAttemptContext attempt, CancellationToken cancellationToken)
    {
        var result = await stateStore.DeleteAsync(
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
