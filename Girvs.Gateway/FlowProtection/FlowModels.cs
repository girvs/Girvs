using System.Text.Json.Serialization;

namespace Girvs.Gateway.FlowProtection;

/// <summary>
/// 流程定义形态：区分“纯线性步骤序列”与“由前置参数选择多条流程组合”的两类流程。
/// </summary>
/// <remarks>
/// 该枚举只决定运行期如何解释 <see cref="FlowDefinition"/> 上的字段，不参与序列化；
/// 具体形态在启动期由配置结构推导，运行期不再重新判定。
/// </remarks>
public enum FlowDefinitionKind
{
    /// <summary>
    /// 线性流程：<see cref="FlowDefinition.Steps"/> 是一条唯一的固定步骤序列，无前置参数。
    /// </summary>
    Linear = 0,

    /// <summary>
    /// 参数化流程：由前置参数选定一条 <see cref="FlowDefinition.Variants"/> 中的流程组合，
    /// 接口池定义在 <see cref="FlowDefinition.Variants"/> 的引用目标上。
    /// </summary>
    Parameterized,
}

/// <summary>
/// 一条流程的完整定义，是运行期校验流程步骤与推进顺位的唯一依据。
/// </summary>
/// <param name="FlowId">流程标识，全局唯一（忽略大小写），票据与该标识绑定。</param>
/// <param name="TtlSeconds">流程票据的存活时长（秒），范围 1 到 1800。</param>
/// <param name="Steps">
/// 线性流程的有序步骤序列；参数化流程固定为空集合，实际步骤由各流程组合提供。
/// </param>
/// <param name="Kind">流程形态，决定运行期读取 <paramref name="Steps" /> 还是 <paramref name="Variants" />。</param>
/// <param name="ParameterHeaderName">
/// 前置参数所在的请求头名；仅参数化流程使用，线性流程为空字符串。
/// </param>
/// <param name="Variants">
/// 前置参数取值到有序流程组合的映射；仅参数化流程非空，键为组合键，忽略大小写。
/// </param>
/// <remarks>
/// 该类型由配置在启动期一次性构建，之后视为不可变并被所有请求线程共享，
/// 因此不提供任何可变成员；运行期状态一律保存在流程票据中。
/// </remarks>
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
    /// <summary>
    /// 以线性流程语义构造定义（等价于 <see cref="FlowDefinitionKind.Linear" />）。
    /// </summary>
    /// <param name="FlowId">流程标识。</param>
    /// <param name="TtlSeconds">流程票据存活时长（秒）。</param>
    /// <param name="Steps">有序步骤序列。</param>
    public FlowDefinition(string FlowId, int TtlSeconds, IReadOnlyList<FlowStepDefinition> Steps)
        : this(FlowId, TtlSeconds, Steps, FlowDefinitionKind.Linear)
    {
    }

    // 保留三值解构签名，兼容既有调用方的源码和二进制。
    /// <summary>
    /// 解构出线性流程的三个基础字段，便于既有调用方保持不变。
    /// </summary>
    /// <param name="FlowId">流程标识。</param>
    /// <param name="TtlSeconds">流程票据存活时长（秒）。</param>
    /// <param name="Steps">有序步骤序列。</param>
    public void Deconstruct(out string FlowId, out int TtlSeconds, out IReadOnlyList<FlowStepDefinition> Steps)
    {
        FlowId = this.FlowId;
        TtlSeconds = this.TtlSeconds;
        Steps = this.Steps;
    }
}

/// <summary>
/// 一个流程步骤的定位信息：HTTP 方法与下游精确路径。
/// </summary>
/// <param name="Method">规范化后的 HTTP 方法（大写）。</param>
/// <param name="Path">
/// 下游服务路径（不含网关服务名前缀），必须以 <c>/</c> 开头且非根路径不以 <c>/</c> 结尾。
/// </param>
/// <remarks>
/// “方法 + 路径”是流程步骤的全局唯一键；同一组合不得跨两条流程归属，
/// 以保证按请求反查流程时结果确定。
/// </remarks>
public sealed record FlowStepDefinition(string Method, PathString Path);

/// <summary>
/// 一条流程组合：由前置参数某个取值选定的完整有序步骤序列。
/// </summary>
/// <param name="Key">组合键，即前置参数的合法取值（规范化后）。</param>
/// <param name="Steps">该组合内有序且互不重复的步骤序列，至少两步。</param>
/// <remarks>
/// 同一流程的不同组合可以复用同一接口并处于不同顺位；锁定组合后即按严格线性序列推进，
/// 不允许在组合之间切换。
/// </remarks>
public sealed record FlowVariantDefinition(string Key, IReadOnlyList<FlowStepDefinition> Steps);

/// <summary>
/// 一个已确定顺位的流程步骤匹配结果，是流程票据推进时比对的目标状态。
/// </summary>
/// <param name="Flow">该步骤所属的流程定义。</param>
/// <param name="Step">命中的步骤定义（方法与下游路径）。</param>
/// <param name="StepIndex">该步骤在所属序列中的零基顺位。</param>
public sealed record FlowStepMatch(FlowDefinition Flow, FlowStepDefinition Step, int StepIndex)
{
    /// <summary>
    /// 该步骤所属流程组合的键；线性流程为空字符串。
    /// </summary>
    public string VariantKey { get; init; } = string.Empty;

    /// <summary>
    /// 该步骤所属序列的总步数。线性流程为空，表示取 <see cref="FlowDefinition.Steps"/> 的长度；
    /// 参数化流程必须赋值，用于正确判定组合内的最后一步。
    /// </summary>
    public int? VariantStepCount { get; init; }

    /// <summary>是否为所属序列的第一步（顺位为零）。</summary>
    public bool IsFirstStep => StepIndex == 0;

    /// <summary>
    /// 是否为所属序列的最后一步；与 <see cref="IsFirstStep"/> 组合可识别单步序列。
    /// </summary>
    public bool IsLastStep => StepIndex == (VariantStepCount ?? Flow.Steps.Count) - 1;
}

/// <summary>
/// 某接口在某个流程组合中的顺位，用于参数化流程的反查与推进。
/// </summary>
/// <param name="VariantKey">流程组合键。</param>
/// <param name="StepIndex">该接口在该组合中的零基顺位。</param>
public sealed record FlowVariantStepIndex(string VariantKey, int StepIndex);

/// <summary>
/// 一个下游接口在流程配置中命中的全部归属信息。
/// </summary>
/// <param name="Flow">该接口所属的流程。</param>
/// <param name="Step">接口定义（方法与下游路径）。</param>
/// <param name="LinearMatch">线性流程命中的步骤匹配；参数化流程为 <see langword="null" />。</param>
/// <param name="VariantIndexes">
/// 该接口出现过的全部（组合键, 顺位）组合；线性流程为空集合。
/// 同一接口可能同时是某组合第 1 步与另一组合的中间步。
/// </param>
/// <remarks>
/// 该类型只在启动期构建、运行期只读，因此按组合键预先建立大小写不敏感的索引，
/// 使每次推进只需一次哈希查找即可还原准确顺位。
/// </remarks>
public sealed record FlowEndpointMatch(
    FlowDefinition Flow,
    FlowStepDefinition Step,
    FlowStepMatch? LinearMatch,
    IReadOnlyList<FlowVariantStepIndex> VariantIndexes
)
{
    /// <summary>
    /// 按组合键（忽略大小写）预建的索引；无候选组合时为 <see langword="null" />，避免空字典分配。
    /// </summary>
    private readonly Dictionary<string, FlowVariantStepIndex>? _indexesByVariant = VariantIndexes.Count == 0
        ? null
        : VariantIndexes.ToDictionary(index => index.VariantKey, StringComparer.OrdinalIgnoreCase);

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
    /// <see cref="FlowStepMatch.IsLastStep"/> 能按组合长度而非线性步骤数判定最后一步。
    /// </remarks>
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
