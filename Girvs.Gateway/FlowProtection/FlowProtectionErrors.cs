namespace Girvs.Gateway.FlowProtection;

/// <summary>
/// 一个可对外输出的流程防护错误描述，用于生成 RFC 7807 问题详情响应。
/// </summary>
/// <param name="Code">
/// 稳定的机器可读错误码，例如 <c>FLOW_STEP_NOT_ALLOWED</c>；客户端应据此分支处理。
/// </param>
/// <param name="Slug">
/// 用于拼接问题详情 <c>type</c> 链接的短标识，例如 <c>step-not-allowed</c>。
/// </param>
/// <param name="Title">面向使用者的简短中文说明，直接作为问题详情的标题。</param>
/// <remarks>
/// 该类型是不可变值对象，所有实例都以静态只读字段形式集中定义在
/// <see cref="FlowProtectionErrors"/> 中，避免各处重复拼装字符串导致错误码漂移。
/// </remarks>
public sealed record FlowProtectionError(
    string Code,
    string Slug,
    string Title
);

/// <summary>
/// 流程防护校验失败时抛出的受控异常；中间件捕获后转换为 403 问题详情响应。
/// </summary>
/// <remarks>
/// 该异常只承载“应当拒绝该请求”的语义，不携带票据内容或业务数据，
/// 因此可以安全地按错误码输出到响应中。
/// </remarks>
public sealed class FlowProtectionException : Exception
{
    /// <summary>
    /// 使用给定的错误描述创建异常，异常消息取该错误的标题。
    /// </summary>
    /// <param name="error">具体的流程防护错误。</param>
    public FlowProtectionException(FlowProtectionError error)
        : base(error.Title)
    {
        Error = error;
    }

    /// <summary>获取触发本次拒绝的错误描述。</summary>
    public FlowProtectionError Error { get; }

    /// <summary>获取错误码，等价于 <see cref="FlowProtectionError.Code"/> 的便捷访问。</summary>
    public string Code => Error.Code;
}

/// <summary>
/// 流程防护错误目录：集中定义对外错误码、问题详情短标识与中文标题，并负责把存储层错误码映射过来。
/// </summary>
/// <remarks>
/// 对外契约（错误码与 <c>type</c> 短标识）一旦发布即应保持稳定，新增场景应追加新条目而不是改写既有条目。
/// </remarks>
public static class FlowProtectionErrors
{
    /// <summary>票据不存在、调用方未携带，或已过期被清理。</summary>
    public static readonly FlowProtectionError NotFound =
        new("FLOW_NOT_FOUND", "not-found", "流程票据不存在或已过期");

    /// <summary>流程状态已过期。</summary>
    public static readonly FlowProtectionError Expired =
        new("FLOW_EXPIRED", "expired", "流程票据已过期");

    /// <summary>调用方提供的业务标识与票据绑定的业务标识不一致。</summary>
    public static readonly FlowProtectionError BusinessMismatch =
        new("FLOW_BUSINESS_MISMATCH", "business-mismatch", "业务标识不匹配");

    /// <summary>当前请求的接口与顺位不是票据允许的下一步（越序、跨组合或持票重调首步）。</summary>
    public static readonly FlowProtectionError StepNotAllowed =
        new("FLOW_STEP_NOT_ALLOWED", "step-not-allowed", "流程步骤不允许执行");

    /// <summary>同一顺位已被其它请求持锁处理中。</summary>
    public static readonly FlowProtectionError InProgress =
        new("FLOW_IN_PROGRESS", "in-progress", "流程步骤正在处理中");

    /// <summary>流程已完成，后续请求被拒绝。</summary>
    public static readonly FlowProtectionError Completed =
        new("FLOW_COMPLETED", "completed", "流程已完成");

    /// <summary>参数化流程新开时，前置参数缺失、空白、同名多值或未命中任何已配置组合。</summary>
    public static readonly FlowProtectionError SelectorInvalid =
        new("FLOW_SELECTOR_INVALID", "selector-invalid", "流程前置参数缺失或非法");

    /// <summary>推进流程时携带的前置参数与票据已锁定的流程组合不一致。</summary>
    public static readonly FlowProtectionError SelectorMismatch =
        new("FLOW_SELECTOR_MISMATCH", "selector-mismatch", "流程前置参数与已锁定流程组合不一致");

    /// <summary>
    /// 把状态存储返回的内部错误码转换为对外错误描述。
    /// </summary>
    /// <param name="errorCode">状态存储返回的错误码。</param>
    /// <returns>对应的对外错误；未识别的取值按“步骤不允许”处理，避免把内部状态细节暴露给客户端。</returns>
    public static FlowProtectionError FromStoreError(FlowErrorCode errorCode)
    {
        return errorCode switch
        {
            FlowErrorCode.FlowNotFound => NotFound,
            FlowErrorCode.FlowExpired => Expired,
            FlowErrorCode.FlowBusinessMismatch => BusinessMismatch,
            FlowErrorCode.FlowStepNotAllowed => StepNotAllowed,
            FlowErrorCode.FlowInProgress => InProgress,
            FlowErrorCode.FlowCompleted => Completed,
            FlowErrorCode.FlowSelectorInvalid => SelectorInvalid,
            FlowErrorCode.FlowSelectorMismatch => SelectorMismatch,
            _ => StepNotAllowed,
        };
    }
}
