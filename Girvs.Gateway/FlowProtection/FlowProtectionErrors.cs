namespace Girvs.Gateway.FlowProtection;

public sealed record FlowProtectionError(
    string Code,
    string Slug,
    string Title
);

public sealed class FlowProtectionException : Exception
{
    public FlowProtectionException(FlowProtectionError error)
        : base(error.Title)
    {
        Error = error;
    }

    public FlowProtectionError Error { get; }

    public string Code => Error.Code;
}

public static class FlowProtectionErrors
{
    public static readonly FlowProtectionError NotFound =
        new("FLOW_NOT_FOUND", "not-found", "流程票据不存在或已过期");

    public static readonly FlowProtectionError Expired =
        new("FLOW_EXPIRED", "expired", "流程票据已过期");

    public static readonly FlowProtectionError BusinessMismatch =
        new("FLOW_BUSINESS_MISMATCH", "business-mismatch", "业务标识不匹配");

    public static readonly FlowProtectionError StepNotAllowed =
        new("FLOW_STEP_NOT_ALLOWED", "step-not-allowed", "流程步骤不允许执行");

    public static readonly FlowProtectionError InProgress =
        new("FLOW_IN_PROGRESS", "in-progress", "流程步骤正在处理中");

    public static readonly FlowProtectionError Completed =
        new("FLOW_COMPLETED", "completed", "流程已完成");

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
            _ => StepNotAllowed,
        };
    }
}
