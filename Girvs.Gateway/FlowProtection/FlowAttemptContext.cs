namespace Girvs.Gateway.FlowProtection;

/// <summary>
/// 单次流程步骤尝试的状态对象：记录本次请求对应的票据、处理锁凭据与已判定顺位。
/// </summary>
/// <remarks>
/// <para>
/// 中间件在预留成功后把本对象写入 <see cref="HttpContext.Items"/>（键为 <see cref="ItemsKey"/>），
/// 供响应阶段（<see cref="FlowResponseTransform"/>）与请求结束时的兜底清理复用，
/// 从而保证“预留”与“提交/释放/删除”严格成对。
/// </para>
/// <para>
/// 本对象只活在一个请求内，不做跨线程共享设计；其中可变的
/// <see cref="NextIndex"/>、<see cref="Completed"/>、<see cref="IsFinalized"/>
/// 由响应阶段按存储返回结果回填。
/// </para>
/// </remarks>
public sealed class FlowAttemptContext
{
    /// <summary>
    /// 在 <see cref="HttpContext.Items"/> 中存放本对象所用的键，确保中间件与响应转换读取同一实例。
    /// </summary>
    public const string ItemsKey = "__GirvsFlowAttemptContext";

    /// <summary>本次尝试所属的流程票据。</summary>
    public required string Ticket { get; init; }

    /// <summary>
    /// 本次尝试的处理锁凭据。提交、释放、删除都必须携带该值，
    /// 存储据此确认操作方确实是当前持锁者。
    /// </summary>
    public required string AttemptId { get; init; }

    /// <summary>本次尝试声明的业务标识；空字符串表示调用方未提供。</summary>
    public required string BusinessId { get; init; }

    /// <summary>本次尝试命中的步骤匹配，含所属流程组合与顺位信息。</summary>
    public required FlowStepMatch Step { get; init; }

    /// <summary>
    /// 本次尝试是否为所属序列的第一步。该值决定失败清理策略：
    /// 首步失败删除整张票据，非首步失败仅释放锁。
    /// </summary>
    public required bool IsFirstStep { get; init; }

    /// <summary>
    /// 存储返回的下一个允许顺位；流程已完成时为最后一步之后的顺位值。
    /// 未产生有效结果（如失败）时可为 <see langword="null" />。
    /// </summary>
    public int? NextIndex { get; set; }

    /// <summary>存储判定本次提交已使流程完成。</summary>
    public bool Completed { get; set; }

    /// <summary>
    /// 本次尝试是否已经完成终态处理（提交、释放或删除成功）。
    /// 未终结的尝试会在请求结束时由中间件兜底清理，防止处理锁残留。
    /// </summary>
    public bool IsFinalized { get; set; }
}
