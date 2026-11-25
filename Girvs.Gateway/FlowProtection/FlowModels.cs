using System.Text.Json.Serialization;

namespace Girvs.Gateway.FlowProtection;

public enum FlowDefinitionKind
{
    Linear = 0,
    Parameterized,
}

[method: JsonConstructor]
public sealed record FlowDefinition(
    string FlowId,
    int TtlSeconds,
    IReadOnlyList<FlowStepDefinition> Steps,
    FlowDefinitionKind Kind = FlowDefinitionKind.Linear,
    string ParameterHeaderName = "",
    IReadOnlyDictionary<string, FlowVariantDefinition>? Variants = null
)
{
    // 保留三参构造签名，兼容既有调用方的源码和二进制。
    public FlowDefinition(string FlowId, int TtlSeconds, IReadOnlyList<FlowStepDefinition> Steps)
        : this(FlowId, TtlSeconds, Steps, FlowDefinitionKind.Linear)
    {
    }

    // 保留三值解构签名，兼容既有调用方的源码和二进制。
    public void Deconstruct(out string FlowId, out int TtlSeconds, out IReadOnlyList<FlowStepDefinition> Steps)
    {
        FlowId = this.FlowId;
        TtlSeconds = this.TtlSeconds;
        Steps = this.Steps;
    }
}

public sealed record FlowStepDefinition(string Method, PathString Path);

public sealed record FlowVariantDefinition(string Key, IReadOnlyList<FlowStepDefinition> Steps);

public sealed record FlowStepMatch(FlowDefinition Flow, FlowStepDefinition Step, int StepIndex)
{
    public string VariantKey { get; init; } = string.Empty;

    public int? VariantStepCount { get; init; }

    public bool IsFirstStep => StepIndex == 0;

    public bool IsLastStep => StepIndex == (VariantStepCount ?? Flow.Steps.Count) - 1;
}

public sealed record FlowVariantStepIndex(string VariantKey, int StepIndex);

public sealed record FlowEndpointMatch(
    FlowDefinition Flow,
    FlowStepDefinition Step,
    FlowStepMatch? LinearMatch,
    IReadOnlyList<FlowVariantStepIndex> VariantIndexes
)
{
    private readonly Dictionary<string, FlowVariantStepIndex>? _indexesByVariant = VariantIndexes.Count == 0
        ? null
        : VariantIndexes.ToDictionary(index => index.VariantKey, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<FlowVariantStepIndex> VariantIndexes { get; } = VariantIndexes;

    public bool TryMatchVariant(string variantKey, out FlowStepMatch match)
    {
        if (_indexesByVariant is not null && _indexesByVariant.TryGetValue(variantKey, out var index)
            && Flow.Variants is not null && Flow.Variants.TryGetValue(index.VariantKey, out var variant))
        {
            match = new FlowStepMatch(Flow, Step, index.StepIndex)
            {
                VariantKey = variant.Key,
                VariantStepCount = variant.Steps.Count,
            };
            return true;
        }

        match = null!;
        return false;
    }
}
