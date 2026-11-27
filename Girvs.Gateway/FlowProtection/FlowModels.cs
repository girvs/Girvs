using System.Text.Json.Serialization;

namespace Girvs.Gateway.FlowProtection;

/// <summary>
/// 一条流程的完整定义，是运行期校验流程步骤与推进顺位的唯一依据。
/// </summary>
/// <param name="FlowId">流程标识，全局唯一（忽略大小写），票据与该标识绑定。</param>
/// <param name="TtlSeconds">流程票据的存活时长（秒），范围 1 到 1800。</param>
/// <param name="ParameterHeaderName">
/// 前置参数所在的请求头名，单组合流程省略时为空字符串。
/// </param>
/// <param name="Variants">
/// 组合键到有序流程组合的非空映射，键忽略大小写。
/// </param>
/// <remarks>
/// 该类型由配置在启动期一次性构建，之后视为不可变并被所有请求线程共享，
/// 因此不提供任何可变成员；运行期状态一律保存在流程票据中。
/// </remarks>
[method: JsonConstructor]
public sealed record FlowDefinition(
    string FlowId,
    int TtlSeconds,
    string ParameterHeaderName,
    IReadOnlyDictionary<string, FlowVariantDefinition> Variants
)
{
    /// <summary>是否配置了前置参数请求头。</summary>
    [JsonIgnore]
    public bool RequiresSelector => !string.IsNullOrWhiteSpace(ParameterHeaderName);

}

/// <summary>
/// 一个流程步骤的定位信息，包括 HTTP 方法和下游静态路径或路径模板。
/// </summary>
/// <param name="Method">规范化后的 HTTP 方法（大写）。</param>
/// <param name="Path">
/// 规范化后的下游服务路径（不含网关服务名前缀）。模板保留配置中的整段 <c>{param}</c>。
/// </param>
/// <remarks>
/// 同一方法下静态路径优先，模板的匹配范围互不重叠，确保请求只命中一个受保护接口。
/// </remarks>
public sealed record FlowStepDefinition(string Method, PathString Path);

/// <summary>
/// 一条流程组合：完整有序的步骤序列，可由前置参数的取值选定。
/// </summary>
/// <param name="Key">组合键，即前置参数的合法取值（规范化后）。</param>
/// <param name="Steps">该组合内有序且互不重复的步骤序列，至少一步。</param>
/// <remarks>
/// 同一流程的不同组合可以复用同一接口并处于不同顺位；锁定组合后即按该组合的步骤顺序推进，
/// 不允许在组合之间切换。
/// </remarks>
public sealed record FlowVariantDefinition(string Key, IReadOnlyList<FlowStepDefinition> Steps);

/// <summary>
/// 一个已确定顺位的流程步骤匹配结果，是流程票据推进时比对的目标状态。
/// </summary>
/// <param name="Flow">该步骤所属的流程定义。</param>
/// <param name="Step">命中的步骤定义（方法与下游路径）。</param>
/// <param name="StepIndex">该步骤在所属序列中的零基顺位。</param>
/// <param name="VariantKey">该步骤所属的非空流程组合键。</param>
/// <param name="VariantStepCount">该步骤所属组合的总步数。</param>
public sealed record FlowStepMatch(
    FlowDefinition Flow,
    FlowStepDefinition Step,
    int StepIndex,
    string VariantKey,
    int VariantStepCount
)
{
    /// <summary>是否为所属序列的第一步（顺位为零）。</summary>
    public bool IsFirstStep => StepIndex == 0;

    /// <summary>
    /// 是否为所属序列的最后一步；与 <see cref="IsFirstStep"/> 组合可识别单步序列。
    /// </summary>
    public bool IsLastStep => StepIndex == VariantStepCount - 1;
}

/// <summary>
/// 某接口在某个流程组合中的顺位，用于流程的反查与推进。
/// </summary>
/// <param name="VariantKey">流程组合键。</param>
/// <param name="StepIndex">该接口在该组合中的零基顺位。</param>
public sealed record FlowVariantStepIndex(string VariantKey, int StepIndex);

/// <summary>
/// 一个下游接口在流程配置中命中的全部归属信息。
/// </summary>
/// <param name="Flow">该接口所属的流程。</param>
/// <param name="Step">接口定义（方法与下游路径）。</param>
/// <param name="VariantIndexes">
/// 该接口出现过的全部（组合键, 顺位）组合，至少一项。
/// 同一接口可能同时是某组合第 1 步与另一组合的中间步。
/// </param>
/// <remarks>
/// 该类型只在启动期构建、运行期只读，因此按组合键预先建立大小写不敏感的索引，
/// 使每次推进只需一次哈希查找即可还原准确顺位。
/// </remarks>
public sealed record FlowEndpointMatch(
    FlowDefinition Flow,
    FlowStepDefinition Step,
    IReadOnlyList<FlowVariantStepIndex> VariantIndexes
)
{
    /// <summary>
    /// 按组合键（忽略大小写）预建的索引。
    /// </summary>
    private readonly Dictionary<string, FlowVariantStepIndex> _indexesByVariant =
        VariantIndexes.ToDictionary(index => index.VariantKey, StringComparer.OrdinalIgnoreCase);

    /// <summary>获取该接口出现过的全部（组合键, 顺位）组合。</summary>
    public IReadOnlyList<FlowVariantStepIndex> VariantIndexes { get; } = VariantIndexes;

    /// <summary>
    /// 尝试用票据已锁定的组合键还原该接口在该组合中的准确步骤匹配。
    /// </summary>
    /// <param name="variantKey">票据锁定或前置参数选定的组合键，忽略大小写匹配。</param>
    /// <param name="match">
    /// 命中时返回带组合键与组合总步数的步骤匹配；未命中时为 <see langword="null" />。
    /// </param>
    /// <returns>该接口确实属于指定组合时返回 <see langword="true" />。</returns>
    /// <remarks>
    /// 这里同时回填 <see cref="FlowStepMatch.VariantStepCount" />，使
    /// <see cref="FlowStepMatch.IsLastStep"/> 能按组合长度判定最后一步。
    /// </remarks>
    public bool TryMatchVariant(string variantKey, out FlowStepMatch match)
    {
        if (_indexesByVariant.TryGetValue(variantKey, out var index)
            && Flow.Variants.TryGetValue(index.VariantKey, out var variant))
        {
            match = new FlowStepMatch(Flow, Step, index.StepIndex, variant.Key, variant.Steps.Count);
            return true;
        }

        match = null!;
        return false;
    }
}
