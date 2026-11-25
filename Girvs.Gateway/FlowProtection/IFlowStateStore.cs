using System.Text.Json.Serialization;

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

[method: JsonConstructor]
public sealed record FlowReserveRequest(
    string Ticket,
    string FlowId,
    string BusinessId,
    int ExpectedIndex,
    string AttemptId,
    int FlowTtlSeconds,
    int LockTtlSeconds,
    string VariantKey = "",
    string SelectorVariantKey = "",
    IReadOnlyList<FlowVariantStepIndex>? CandidateVariantSteps = null
)
{
    // 保留七参构造签名，兼容既有调用方的源码和二进制。
    public FlowReserveRequest(
        string Ticket, string FlowId, string BusinessId, int ExpectedIndex, string AttemptId,
        int FlowTtlSeconds, int LockTtlSeconds
    ) : this(Ticket, FlowId, BusinessId, ExpectedIndex, AttemptId, FlowTtlSeconds, LockTtlSeconds, "")
    {
    }

    // 保留七值解构签名，兼容既有调用方的源码和二进制。
    public void Deconstruct(
        out string Ticket, out string FlowId, out string BusinessId, out int ExpectedIndex, out string AttemptId,
        out int FlowTtlSeconds, out int LockTtlSeconds
    )
    {
        Ticket = this.Ticket;
        FlowId = this.FlowId;
        BusinessId = this.BusinessId;
        ExpectedIndex = this.ExpectedIndex;
        AttemptId = this.AttemptId;
        FlowTtlSeconds = this.FlowTtlSeconds;
        LockTtlSeconds = this.LockTtlSeconds;
    }
}

public sealed record FlowCommitRequest(
    string Ticket,
    string AttemptId,
    int ExpectedIndex,
    bool IsLastStep,
    string BusinessIdFromResponse,
    int CompletedTtlSeconds
);

public sealed record FlowReleaseRequest(string Ticket, string AttemptId);

[method: JsonConstructor]
public sealed record FlowStoreResult(
    bool IsSuccess,
    FlowErrorCode? ErrorCode,
    int? NextIndex,
    bool Completed,
    string? VariantKey = null
)
{
    // 保留四参构造签名，兼容既有调用方的源码和二进制。
    public FlowStoreResult(bool isSuccess, FlowErrorCode? errorCode, int? nextIndex, bool completed)
        : this(isSuccess, errorCode, nextIndex, completed, null)
    {
    }

    // 保留四值解构签名，兼容既有调用方的源码和二进制。
    public void Deconstruct(
        out bool isSuccess, out FlowErrorCode? errorCode, out int? nextIndex, out bool completed
    )
    {
        isSuccess = IsSuccess;
        errorCode = ErrorCode;
        nextIndex = NextIndex;
        completed = Completed;
    }

    public static FlowStoreResult Success(int? nextIndex = null, bool completed = false)
    {
        return new FlowStoreResult(true, null, nextIndex, completed);
    }

    public static FlowStoreResult Success(int? nextIndex, bool completed, string? variantKey)
    {
        return new FlowStoreResult(true, null, nextIndex, completed, variantKey);
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
    FlowSelectorInvalid,
    FlowSelectorMismatch,
}
