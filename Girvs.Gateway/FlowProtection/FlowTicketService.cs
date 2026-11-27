using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Girvs.Gateway.FlowProtection;

/// <summary>
/// 票据服务：把中间件与响应阶段的业务语义翻译为状态存储的原子操作，并维护处理锁时长的统一来源。
/// </summary>
/// <remarks>
/// <para>
/// 本类型不持有任何请求状态，所有状态都在参数中显式传递，因此可安全注册为单例并并发调用。
/// </para>
/// <para>
/// 票据与处理锁凭据均由加密安全随机数生成，并以 URL 安全字符编码，
/// 可直接放在请求头中传输而无需额外转义。
/// </para>
/// </remarks>
public sealed class FlowTicketService
{
    /// <summary>流程票据状态的存储实现。</summary>
    private readonly IFlowStateStore _stateStore;

    /// <summary>处理锁时长（秒），随每次预留请求下发给存储。</summary>
    private readonly int _lockTtlSeconds;

    /// <summary>
    /// 使用默认处理锁时长（30 秒）创建服务。
    /// </summary>
    /// <param name="stateStore">流程票据状态的存储实现。</param>
    public FlowTicketService(IFlowStateStore stateStore)
        : this(stateStore, lockTtlSeconds: 30)
    {
    }

    /// <summary>
    /// 使用指定处理锁时长创建服务。
    /// </summary>
    /// <param name="stateStore">流程票据状态的存储实现。</param>
    /// <param name="lockTtlSeconds">处理锁时长（秒），应大于 0 且不超过票据存活时长。</param>
    public FlowTicketService(IFlowStateStore stateStore, int lockTtlSeconds)
    {
        _stateStore = stateStore;
        _lockTtlSeconds = lockTtlSeconds;
    }

    /// <summary>
    /// 生成一个新的流程票据。
    /// </summary>
    /// <returns>URL 安全的票据字符串。</returns>
    public string CreateTicket()
    {
        return CreateRandomToken();
    }

    /// <summary>
    /// 生成一个新的处理锁凭据。
    /// </summary>
    /// <returns>URL 安全的凭据字符串。</returns>
    /// <remarks>
    /// 每次尝试都使用新凭据，使存储侧能精确判断“当前持锁者是谁”，
    /// 从而拒绝已被超时剥夺的旧尝试提交结果。
    /// </remarks>
    public string CreateAttemptId()
    {
        return CreateRandomToken();
    }

    /// <summary>
    /// 开启新流程时创建票据、锁定组合并占用首步。
    /// </summary>
    /// <param name="businessId">调用方声明的业务标识；可为空，后续可由下游响应补齐。</param>
    /// <param name="step">命中的首步匹配，提供流程标识、存活时长与组合键。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>已预留首步的尝试状态对象。</returns>
    /// <exception cref="FlowProtectionException">存储拒绝创建（例如随机票据极少概率碰撞）。</exception>
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

    /// <summary>
    /// 按票据已锁定的流程组合预留下一步，由状态存储原子校验候选顺位与可选前置参数，
    /// 并用返回的命中组合还原准确的 <see cref="FlowStepMatch" />（含该组合的最后一步判定）。
    /// </summary>
    /// <param name="ticket">流程票据。</param>
    /// <param name="businessId">调用方声明的业务标识，不能为空。</param>
    /// <param name="endpoint">命中的接口归属信息，提供全部候选组合顺位。</param>
    /// <param name="selectorVariantKey">
    /// 调用方携带的前置参数（已归一为组合键）；未携带时为 <see langword="null" />，
    /// 由存储只按锁定组合校验。
    /// </param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>已预留目标步骤的尝试状态对象，其步骤匹配按命中组合重建。</returns>
    /// <exception cref="FlowProtectionException">组合不一致、顺位不允许、正在处理中，或存储返回的组合与配置不一致。</exception>
    /// <remarks>
    /// 存储脚本按锁定组合选取候选顺位，
    /// 这样同一接口在不同组合中处于不同顺位时也能正确推进。
    /// </remarks>
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

    /// <summary>
    /// 提交当前步骤的成功结果并推进顺位。
    /// </summary>
    /// <param name="attempt">当前尝试状态对象，提交成功后会被回填为已终结并更新顺位与完成标记。</param>
    /// <param name="businessIdFromResponse">下游回传的业务标识；为空表示本次不绑定或比对。</param>
    /// <param name="completedTtlSeconds">完成态保留时长（秒），仅在最后一步生效。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>存储的提交结果。</returns>
    /// <exception cref="FlowProtectionException">处理锁已失效、顺位不符或业务标识冲突。</exception>
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
        // 标记终结，告知中间件无需兜底清理。
        attempt.IsFinalized = true;
        return result;
    }

    /// <summary>
    /// 释放当前步骤的处理锁，票据与顺位保持不变。
    /// </summary>
    /// <param name="attempt">当前尝试状态对象，释放成功后会被标记为已终结。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <exception cref="FlowProtectionException">处理锁凭据不匹配（例如锁已被超时剥夺）。</exception>
    public async Task ReleaseAsync(FlowAttemptContext attempt, CancellationToken cancellationToken)
    {
        var result = await _stateStore.ReleaseAsync(
            new FlowReleaseRequest(attempt.Ticket, attempt.AttemptId),
            cancellationToken
        );
        EnsureSuccess(result);
        attempt.IsFinalized = true;
    }

    /// <summary>
    /// 删除整张票据，用于撤销以首步失败的流程。
    /// </summary>
    /// <param name="attempt">当前尝试状态对象，删除成功后会被标记为已终结。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <exception cref="FlowProtectionException">处理锁凭据不匹配，或票据已不存在。</exception>
    public async Task DeleteAsync(FlowAttemptContext attempt, CancellationToken cancellationToken)
    {
        var result = await _stateStore.DeleteAsync(
            new FlowReleaseRequest(attempt.Ticket, attempt.AttemptId),
            cancellationToken
        );
        EnsureSuccess(result);
        attempt.IsFinalized = true;
    }

    /// <summary>
    /// 计算票据的短指纹，用于日志与排障时不泄露票据本体。
    /// </summary>
    /// <param name="ticket">流程票据。</param>
    /// <returns>票据的 SHA-256 十六进制摘要前 12 位（小写）。</returns>
    /// <remarks>
    /// 票据本身是持有者凭据，明文写入日志等同于泄露授权，因此对外记录一律使用该指纹。
    /// </remarks>
    public static string GetTicketFingerprint(string ticket)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(ticket));
        return Convert.ToHexString(hash).ToLowerInvariant()[..12];
    }

    /// <summary>
    /// 尽力释放处理锁，失败时静默忽略。
    /// </summary>
    /// <param name="ticket">流程票据。</param>
    /// <param name="attemptId">处理锁凭据。</param>
    /// <param name="cancellationToken">取消标记。</param>
    /// <remarks>
    /// 用于“已经判定失败、但仍需回收锁”的收尾路径；释放本身失败不能掩盖真正的失败原因，
    /// 残留的锁会在租约到期后自动失效。
    /// </remarks>
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

    /// <summary>
    /// 生成 32 字节加密安全随机数并编码为 URL 安全字符串。
    /// </summary>
    /// <returns>可直接放入请求头的随机字符串。</returns>
    private static string CreateRandomToken()
    {
        return WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
    }

    /// <summary>
    /// 断言存储操作成功，否则把存储错误码转换为对外异常。
    /// </summary>
    /// <param name="result">存储返回结果。</param>
    /// <exception cref="FlowProtectionException">结果为失败。</exception>
    /// <remarks>
    /// 错误码缺失时按“步骤不允许”处理，保持失败路径始终被拒绝、绝不误放行。
    /// </remarks>
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
