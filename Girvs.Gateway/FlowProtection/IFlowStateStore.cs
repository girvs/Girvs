using System.Text.Json.Serialization;

namespace Girvs.Gateway.FlowProtection;

/// <summary>
/// 流程票据状态存储的抽象，负责以原子方式完成“预留 → 提交/释放/删除”的状态机迁移。
/// </summary>
/// <remarks>
/// <para>
/// 所有实现都必须保证单个操作内部不可被打断（例如在 Redis 中用 Lua 脚本执行），
/// 否则并发请求可能同时通过同一顺位的校验，破坏顺序约束。
/// </para>
/// <para>
/// 返回统一为 <see cref="FlowStoreResult"/>：校验不通过不抛异常，而是返回带错误码的失败结果，
/// 由上层转换为对外错误响应；只有基础设施层故障（连接失败、脚本异常）才允许抛异常。
/// </para>
/// </remarks>
public interface IFlowStateStore
{
    /// <summary>
    /// 为新流程创建票据并立即占用首步。若票据已存在，视为重复开启同一流程而拒绝。
    /// </summary>
    /// <param name="request">预留请求，必须携带完整流程与顺位信息。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>成功时含下一个允许顺位（与是否完成）；失败时含具体错误码。</returns>
    Task<FlowStoreResult> CreateAndReserveAsync(
        FlowReserveRequest request,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// 为已存在的票据占用下一步。校验票据归属、业务标识、组合与顺位，并加处理锁。
    /// </summary>
    /// <param name="request">预留请求，须含票据、业务标识与目标顺位或组合候选。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>成功时含命中的组合键与下一个允许顺位；失败时含具体错误码。</returns>
    Task<FlowStoreResult> ReserveAsync(FlowReserveRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 将当前步骤标记为成功完成，并推进顺位；最后一步会转为完成态。
    /// </summary>
    /// <param name="request">提交请求，须含处理锁凭据与最终顺位信息。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>成功时含推进后的顺位与完成标记。</returns>
    Task<FlowStoreResult> CommitAsync(FlowCommitRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 释放当前步骤的处理锁，使该顺位可被重新尝试（票据本身保留）。
    /// </summary>
    /// <param name="request">释放请求，须含处理锁凭据以确认操作方。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>成功时含未变化的当前顺位。</returns>
    Task<FlowStoreResult> ReleaseAsync(FlowReleaseRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// 删除整张票据，用于首步失败时撤销本次流程，避免留下无法继续的悬挂状态。
    /// </summary>
    /// <param name="request">删除请求，须含处理锁凭据以确认操作方。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>成功时不携带顺位语义；票据已不存在时返回未找到错误码。</returns>
    Task<FlowStoreResult> DeleteAsync(FlowReleaseRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// 一次“预留”操作的请求参数，同时用于开启新流程与推进既有流程。
/// </summary>
/// <param name="Ticket">流程票据；开启新流程时由网关生成。</param>
/// <param name="FlowId">票据必须归属的流程标识，用于校验票据与配置的一致性。</param>
/// <param name="BusinessId">调用方声明的业务标识；为空表示未提供。</param>
/// <param name="ExpectedIndex">
/// 期望的当前顺位。线性流程直接比对；参数化流程在给出组合候选时该值被候选顺位取代。
/// </param>
/// <param name="AttemptId">本次处理锁凭据，后续提交/释放/删除需回传该值。</param>
/// <param name="FlowTtlSeconds">票据存活时长（秒），用于首次写入时设置过期。</param>
/// <param name="LockTtlSeconds">处理锁时长（秒），超时后锁自然失效。</param>
/// <param name="VariantKey">开启时锁定的流程组合键；线性流程为空字符串。</param>
/// <param name="SelectorVariantKey">
/// 推进时调用方携带的前置参数（已归一为组合键），空字符串表示未携带；
/// 与锁定组合不一致时由存储返回组合不匹配。
/// </param>
/// <param name="CandidateVariantSteps">
/// 当前接口在各流程组合中的候选顺位；参数化流程推进必须提供，线性流程为 <see langword="null" />。
/// </param>
/// <remarks>
/// 该类型同时被序列化用于跨进程传输，因此保留完整的 JSON 构造入口；
/// 后加的字段全部带默认值，保证既有调用方的七参构造与解构行为不变。
/// </remarks>
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
    /// <summary>
    /// 以线性流程语义构造请求（组合相关字段取默认空值）。
    /// </summary>
    /// <param name="Ticket">流程票据。</param>
    /// <param name="FlowId">流程标识。</param>
    /// <param name="BusinessId">业务标识。</param>
    /// <param name="ExpectedIndex">期望的当前顺位。</param>
    /// <param name="AttemptId">处理锁凭据。</param>
    /// <param name="FlowTtlSeconds">票据存活时长（秒）。</param>
    /// <param name="LockTtlSeconds">处理锁时长（秒）。</param>
    public FlowReserveRequest(
        string Ticket, string FlowId, string BusinessId, int ExpectedIndex, string AttemptId,
        int FlowTtlSeconds, int LockTtlSeconds
    ) : this(Ticket, FlowId, BusinessId, ExpectedIndex, AttemptId, FlowTtlSeconds, LockTtlSeconds, "")
    {
    }

    // 保留七值解构签名，兼容既有调用方的源码和二进制。
    /// <summary>
    /// 解构出线性流程所需的七个基础字段，便于既有调用方保持不变。
    /// </summary>
    /// <param name="Ticket">流程票据。</param>
    /// <param name="FlowId">流程标识。</param>
    /// <param name="BusinessId">业务标识。</param>
    /// <param name="ExpectedIndex">期望的当前顺位。</param>
    /// <param name="AttemptId">处理锁凭据。</param>
    /// <param name="FlowTtlSeconds">票据存活时长（秒）。</param>
    /// <param name="LockTtlSeconds">处理锁时长（秒）。</param>
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

/// <summary>
/// 一次“提交”操作的请求参数：把当前步骤从“处理中”推进为“已完成本步”。
/// </summary>
/// <param name="Ticket">流程票据。</param>
/// <param name="AttemptId">当前处理锁凭据；与存储中的值不一致时提交被拒绝。</param>
/// <param name="ExpectedIndex">提交前的当前顺位；用于确认仍是同一顺位。</param>
/// <param name="IsLastStep">是否为所属序列的最后一步；为真时流程转为完成态。</param>
/// <param name="BusinessIdFromResponse">
/// 下游响应回传的业务标识；非空时用于首次绑定或与已绑定值比对。
/// </param>
/// <param name="CompletedTtlSeconds">完成态的保留时长（秒），仅在最后一步完成时生效。</param>
public sealed record FlowCommitRequest(
    string Ticket,
    string AttemptId,
    int ExpectedIndex,
    bool IsLastStep,
    string BusinessIdFromResponse,
    int CompletedTtlSeconds
);

/// <summary>
/// 一次“释放锁”或“删除票据”操作的请求参数。
/// </summary>
/// <param name="Ticket">流程票据。</param>
/// <param name="AttemptId">当前处理锁凭据；与存储中的值不一致时操作被拒绝，保证只有持锁者能改变状态。</param>
public sealed record FlowReleaseRequest(string Ticket, string AttemptId);

/// <summary>
/// 状态存储操作的统一结果：成功时携带推进后的顺位与完成标记，失败时携带错误码。
/// </summary>
/// <param name="IsSuccess">操作是否被接受。</param>
/// <param name="ErrorCode">失败时的具体错误码；成功时为 <see langword="null" />。</param>
/// <param name="NextIndex">
/// 成功时下一个允许执行的顺位；操作未产生顺位语义时为 <see langword="null" />。
/// 流程完成时为最后一步之后的顺位值。
/// </param>
/// <param name="Completed">成功时表示流程是否已进入完成态。</param>
/// <param name="VariantKey">
/// 参数化流程推进时命中的组合键（已规范化）；线性流程或未命中时为 <see langword="null" />。
/// </param>
/// <remarks>
/// 该类型同时用于跨进程传输，因此保留完整 JSON 构造入口与三个静态工厂方法，
/// 并用四参构造/解构重载维持既有调用方的源码与二进制兼容。
/// </remarks>
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
    /// <summary>
    /// 以不带组合键的语义构造结果，等价于 <see cref="VariantKey"/> 为 <see langword="null" />。
    /// </summary>
    /// <param name="isSuccess">操作是否成功。</param>
    /// <param name="errorCode">失败时的错误码。</param>
    /// <param name="nextIndex">下一个允许顺位。</param>
    /// <param name="completed">流程是否已完成。</param>
    public FlowStoreResult(bool isSuccess, FlowErrorCode? errorCode, int? nextIndex, bool completed)
        : this(isSuccess, errorCode, nextIndex, completed, null)
    {
    }

    // 保留四值解构签名，兼容既有调用方的源码和二进制。
    /// <summary>
    /// 解构出四个基础字段（不含组合键），便于既有调用方保持不变。
    /// </summary>
    /// <param name="isSuccess">操作是否成功。</param>
    /// <param name="errorCode">失败时的错误码。</param>
    /// <param name="nextIndex">下一个允许顺位。</param>
    /// <param name="completed">流程是否已完成。</param>
    public void Deconstruct(
        out bool isSuccess, out FlowErrorCode? errorCode, out int? nextIndex, out bool completed
    )
    {
        isSuccess = IsSuccess;
        errorCode = ErrorCode;
        nextIndex = NextIndex;
        completed = Completed;
    }

    /// <summary>
    /// 构造一个不带组合键的成功结果。
    /// </summary>
    /// <param name="nextIndex">下一个允许顺位；无顺位语义时传 <see langword="null" />。</param>
    /// <param name="completed">流程是否已完成。</param>
    /// <returns>成功结果实例。</returns>
    public static FlowStoreResult Success(int? nextIndex = null, bool completed = false)
    {
        return new FlowStoreResult(true, null, nextIndex, completed);
    }

    /// <summary>
    /// 构造一个携带命中组合键的成功结果，供参数化流程还原正确顺位。
    /// </summary>
    /// <param name="nextIndex">下一个允许顺位。</param>
    /// <param name="completed">流程是否已完成。</param>
    /// <param name="variantKey">命中的流程组合键；为空表示未命中。</param>
    /// <returns>成功结果实例。</returns>
    public static FlowStoreResult Success(int? nextIndex, bool completed, string? variantKey)
    {
        return new FlowStoreResult(true, null, nextIndex, completed, variantKey);
    }

    /// <summary>
    /// 构造一个失败结果。
    /// </summary>
    /// <param name="errorCode">具体错误码，上层据此映射为对外响应。</param>
    /// <returns>失败结果实例。</returns>
    public static FlowStoreResult Failed(FlowErrorCode errorCode)
    {
        return new FlowStoreResult(false, errorCode, null, false);
    }
}

/// <summary>
/// 状态存储内部错误码，用于在“存储层”与“对外错误目录”之间解耦传递。
/// </summary>
public enum FlowErrorCode
{
    /// <summary>票据不存在（未携带或已被清理）。</summary>
    FlowNotFound,

    /// <summary>流程状态已过期。</summary>
    FlowExpired,

    /// <summary>业务标识与票据绑定值不一致。</summary>
    FlowBusinessMismatch,

    /// <summary>当前接口或顺位不是票据允许的下一步。</summary>
    FlowStepNotAllowed,

    /// <summary>同一顺位已有未过期的处理锁。</summary>
    FlowInProgress,

    /// <summary>流程已完成。</summary>
    FlowCompleted,

    /// <summary>参数化流程新开时前置参数缺失、非法或未命中任何组合。</summary>
    FlowSelectorInvalid,

    /// <summary>推进时携带的前置参数与票据已锁定的流程组合不一致。</summary>
    FlowSelectorMismatch,
}
