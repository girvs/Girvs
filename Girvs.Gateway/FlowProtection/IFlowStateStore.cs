namespace Girvs.Gateway.FlowProtection;

public interface IFlowStateStore
{
    Task<FlowStoreResult> CreateAndReserveAsync(
        FlowReserveRequest request,
        CancellationToken cancellationToken
    );

    Task<FlowStoreResult> ReserveAsync(FlowReserveRequest request, CancellationToken cancellationToken);

    Task<FlowStoreResult> CommitAsync(FlowCommitRequest request, CancellationToken cancellationToken);

    Task<FlowStoreResult> ReleaseAsync(FlowReleaseRequest request, CancellationToken cancellationToken);

    Task<FlowStoreResult> DeleteAsync(FlowReleaseRequest request, CancellationToken cancellationToken);
}

public sealed record FlowReserveRequest(
    string Ticket,
    string FlowId,
    string BusinessId,
    int ExpectedIndex,
    string AttemptId,
    int FlowTtlSeconds,
    int LockTtlSeconds
);

public sealed record FlowCommitRequest(
    string Ticket,
    string AttemptId,
    int ExpectedIndex,
    bool IsLastStep,
    string BusinessIdFromResponse,
    int CompletedTtlSeconds
);

public sealed record FlowReleaseRequest(string Ticket, string AttemptId);

public sealed record FlowStoreResult(
    bool IsSuccess,
    FlowErrorCode? ErrorCode,
    int? NextIndex,
    bool Completed
)
{
    public static FlowStoreResult Success(int? nextIndex, bool completed)
    {
        return new FlowStoreResult(true, null, nextIndex, completed);
    }

    public static FlowStoreResult Failed(FlowErrorCode errorCode)
    {
        return new FlowStoreResult(false, errorCode, null, false);
    }
}

public enum FlowErrorCode
{
    FlowNotFound,
    FlowExpired,
    FlowBusinessMismatch,
    FlowStepNotAllowed,
    FlowInProgress,
    FlowCompleted,
}
