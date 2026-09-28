namespace Girvs.Gateway.FlowProtection;

public sealed record FlowDefinition(
    string FlowId,
    int TtlSeconds,
    IReadOnlyList<FlowStepDefinition> Steps
);

public sealed record FlowStepDefinition(string Method, PathString Path);

public sealed record FlowStepMatch(FlowDefinition Flow, FlowStepDefinition Step, int StepIndex)
{
    public bool IsFirstStep => StepIndex == 0;

    public bool IsLastStep => StepIndex == Flow.Steps.Count - 1;
}
