namespace Girvs.Gateway.FlowProtection;

public sealed class FlowAttemptContext
{
    public const string ItemsKey = "__GirvsFlowAttemptContext";

    public required string Ticket { get; init; }

    public required string AttemptId { get; init; }

    public required string BusinessId { get; init; }

    public required FlowStepMatch Step { get; init; }

    public required bool IsFirstStep { get; init; }

    public int? NextIndex { get; set; }

    public bool Completed { get; set; }

    public bool IsFinalized { get; set; }
}
