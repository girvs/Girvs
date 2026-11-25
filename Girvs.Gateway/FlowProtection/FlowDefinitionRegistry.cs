using Girvs;
using Girvs.Gateway.FlowProtection.Configuration;

namespace Girvs.Gateway.FlowProtection;

/// <summary>
/// 流程定义注册表：在启动期校验配置、编译为只读流程定义，并提供“方法 + 下游路径”到接口归属的确定性反查。
/// </summary>
/// <remarks>
/// <para>
/// 本类型承担两类职责：一是把易错的配置结构（线性步骤序列 / 参数化接口池 + 流程组合）
/// 校验并翻译成运行期使用的 <see cref="FlowDefinition"/> / <see cref="FlowEndpointMatch"/>；
/// 二是在运行期为每个受保护请求提供 O(1) 的步骤反查。
/// </para>
/// <para>
/// 构造完成后内部集合不再变更，因此可安全地注册为单例并被所有请求线程并发读取。
/// 校验失败一律抛 <see cref="GirvsException"/>，把问题暴露在启动期而不是运行期。
/// </para>
/// <para>
/// 核心不变量：同一 “HTTP 方法 + 下游路径”只能归属唯一一条流程；同一流程内步骤不得重复；
/// 参数化流程中每个接口至少被一条流程组合引用。
/// </para>
/// </remarks>
public sealed class FlowDefinitionRegistry
{
    /// <summary>
    /// 允许出现在流程步骤中的 HTTP 方法白名单；其它谓词（如 TRACE）一律视为配置错误。
    /// </summary>
    private static readonly HashSet<string> AllowedMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "GET",
        "POST",
        "PUT",
        "DELETE",
        "PATCH",
        "HEAD",
        "OPTIONS",
    };

    /// <summary>
    /// 运行期反查表：键为规范化的 “METHOD 空格 路径”，值为该接口的完整归属信息。
    /// 键比较忽略大小写，保证不同书写风格的方法名/路径不产生重复归属。
    /// </summary>
    private readonly Dictionary<string, FlowEndpointMatch> _endpointsByMethodAndPath =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 校验并编译流程防护配置。
    /// </summary>
    /// <param name="options">流程防护配置。</param>
    /// <exception cref="ArgumentNullException"><paramref name="options" /> 为 <see langword="null" />。</exception>
    /// <exception cref="GirvsException">
    /// 任一校验规则不满足：存活时长越界、处理锁时长非法、流程标识重复、
    /// 步骤与参数化配置混用、步骤数不足、接口未被引用、路径或方法非法、接口被多条流程复用等。
    /// </exception>
    /// <remarks>
    /// 校验顺序刻意保持稳定：先全局参数（各存活时长与处理锁），再逐条流程。
    /// 这样配置中存在多处问题时，报错总是指向最先出现的全局性错误，便于一次性修正。
    /// </remarks>
    public FlowDefinitionRegistry(FlowProtectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // 全局参数先行校验：后续流程校验会引用这些值（尤其是回退用的默认存活时长）。
        ValidateTtl(nameof(options.DefaultTtlSeconds), options.DefaultTtlSeconds);
        ValidateTtl(nameof(options.CompletedTtlSeconds), options.CompletedTtlSeconds);
        // 处理锁时长必须为正，且不得长于票据存活时长，否则票据已过期而锁仍在，语义不自洽。
        if (options.LockTtlSeconds <= 0 || options.LockTtlSeconds > options.DefaultTtlSeconds)
        {
            throw new GirvsException("FlowProtection LockTtlSeconds 必须大于 0 且不超过 DefaultTtlSeconds");
        }

        var flowIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var flowOptions in options.Flows ?? [])
        {
            if (flowOptions is null)
            {
                throw new GirvsException("FlowProtection Flows 不能包含空项");
            }

            // 流程标识必须存在且全局唯一，运行期票据正是按该标识校验归属。
            var flowId = RequireValue(flowOptions.FlowId, "FlowId 不能为空");
            if (!flowIds.Add(flowId))
            {
                throw new GirvsException($"FlowId '{flowId}' 重复");
            }

            // 配置绑定会把显式 null 覆盖到集合属性上，这里统一按空集合处理。
            var stepOptionsList = flowOptions.Steps ?? [];
            var interfaces = flowOptions.Interfaces ?? [];
            var variants = flowOptions.Variants ?? [];
            // 形态判定：任意参数化字段非空即视为参数化流程，否则按线性流程处理。
            var hasLinear = stepOptionsList.Count > 0;
            var hasParameterized = !string.IsNullOrWhiteSpace(flowOptions.ParameterHeaderName)
                || interfaces.Count > 0 || variants.Count > 0;
            if (hasLinear && hasParameterized)
            {
                throw new GirvsException($"流程 '{flowId}' 不能同时配置 Steps 和前置参数组合");
            }

            // 单条流程未指定存活时长时回退到全局默认值。
            var ttlSeconds = flowOptions.TtlSeconds ?? options.DefaultTtlSeconds;
            if (hasParameterized)
            {
                ValidateTtl($"流程 '{flowId}' TtlSeconds", ttlSeconds);
                var header = ValidateParameterHeader(options, flowId, flowOptions.ParameterHeaderName);
                RegisterParameterized(flowId, ttlSeconds, header, interfaces, variants);
            }
            else
            {
                RegisterLinear(flowId, ttlSeconds, stepOptionsList);
            }
        }
    }

    /// <summary>
    /// 仅查找纯线性流程的步骤。参数化端点返回 <see langword="false" />。
    /// 新代码应使用 <see cref="TryGetEndpoint" />。
    /// </summary>
    /// <param name="method">请求的 HTTP 方法。</param>
    /// <param name="path">还原后的下游服务路径。</param>
    /// <param name="match">命中时返回线性步骤匹配；否则为 <see langword="null" />。</param>
    /// <returns>命中的是线性流程步骤时返回 <see langword="true" />。</returns>
    /// <remarks>
    /// 保留该重载只是为了兼容既有调用方；参数化流程在这里一律视为未命中，
    /// 因此新代码必须使用 <see cref="TryGetEndpoint"/> 才能正确处理流程组合。
    /// </remarks>
    public bool TryGetStep(string method, PathString path, out FlowStepMatch match)
    {
        match = TryGetEndpoint(method, path, out var endpoint) ? endpoint.LinearMatch! : null!;
        return match is not null;
    }

    /// <summary>
    /// 按请求的方法与下游路径反查接口归属，包含线性与参数化两类流程。
    /// </summary>
    /// <param name="method">请求的 HTTP 方法，比较时忽略大小写。</param>
    /// <param name="path">还原后的下游服务路径，查找前会做与配置一致的规范化（去尾斜杠）。</param>
    /// <param name="match">命中时返回接口归属信息；否则为 <see langword="null" />。</param>
    /// <returns>该接口受流程防护约束时返回 <see langword="true" />；未配置保护时返回 <see langword="false" />，调用方应直接放行。</returns>
    /// <remarks>
    /// 查询路径同样需要规范化，否则客户端请求 <c>/api/pay1/</c> 与配置
    /// <c>/api/pay1</c> 会被误判为不同接口，造成保护失效。
    /// </remarks>
    public bool TryGetEndpoint(string method, PathString path, out FlowEndpointMatch match)
    {
        var normalizedPath = NormalizePath(path.Value ?? string.Empty, "request");
        return _endpointsByMethodAndPath.TryGetValue(CreateKey(method, normalizedPath), out match!);
    }

    /// <summary>
    /// 校验并注册一条线性流程的全部步骤。
    /// </summary>
    /// <param name="flowId">流程标识。</param>
    /// <param name="ttlSeconds">该流程票据的存活时长（秒）。</param>
    /// <param name="stepOptionsList">按顺序排列的步骤配置。</param>
    /// <exception cref="GirvsException">
    /// 步骤少于两步、步骤为空项、方法或路径非法、或同一流程内步骤重复。
    /// </exception>
    /// <remarks>
    /// 线性流程要求至少两步：单步流程不存在“顺序约束”语义，配置出来只会让首步即完成，
    /// 属于典型的误配置，因此直接拒绝。
    /// </remarks>
    private void RegisterLinear(string flowId, int ttlSeconds, List<FlowStepOptions> stepOptionsList)
    {
        if (stepOptionsList.Count < 2)
        {
            throw new GirvsException($"流程 '{flowId}' 至少需要两个步骤");
        }

        ValidateTtl($"流程 '{flowId}' TtlSeconds", ttlSeconds);

        var flowStepKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var steps = new List<FlowStepDefinition>(stepOptionsList.Count);
        foreach (var stepOptions in stepOptionsList)
        {
            if (stepOptions is null)
            {
                throw new GirvsException($"流程 '{flowId}' Steps 不能包含空项");
            }

            var method = NormalizeMethod(stepOptions.Method, flowId);
            var path = NormalizePath(stepOptions.Path, flowId);
            var key = CreateKey(method, path);
            // 同一流程内重复注册同一接口会让顺位判定产生歧义，必须拒绝。
            if (!flowStepKeys.Add(key))
            {
                throw new GirvsException($"流程 '{flowId}' 中步骤 '{method} {path}' 重复");
            }

            steps.Add(new FlowStepDefinition(method, new PathString(path)));
        }

        var flow = new FlowDefinition(flowId, ttlSeconds, steps);
        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            // 线性流程的每个步骤都同时记录线性匹配与空的组合索引。
            RegisterEndpoint(new FlowEndpointMatch(flow, step, new FlowStepMatch(flow, step, i), []));
        }
    }

    /// <summary>
    /// 校验并注册一条参数化流程：构建接口池、编译各流程组合，并为每个接口建立组合顺位索引。
    /// </summary>
    /// <param name="flowId">流程标识。</param>
    /// <param name="ttlSeconds">该流程票据的存活时长（秒）。</param>
    /// <param name="header">已通过保留头冲突校验的前置参数请求头名。</param>
    /// <param name="interfaceOptions">命名接口池配置。</param>
    /// <param name="variantOptions">组合键到接口引用列表的映射。</param>
    /// <exception cref="GirvsException">
    /// 组合为空、组合键重复或为空、组合步骤少于两步、引用未定义或重复引用接口、
    /// 或存在未被任何组合引用的孤立接口。
    /// </exception>
    /// <remarks>
    /// 组合内步骤顺序即接口引用顺序；同一接口可在不同组合中处于不同顺位，
    /// 因此这里为每个接口累积“组合键 → 顺位”的全部候选，供运行期按票据锁定组合精确还原顺位。
    /// </remarks>
    private void RegisterParameterized(
        string flowId,
        int ttlSeconds,
        string header,
        List<FlowInterfaceOptions> interfaceOptions,
        Dictionary<string, List<string>> variantOptions
    )
    {
        // 先建接口池：后续组合只能引用池内已定义且唯一的方法+路径。
        var interfaces = BuildInterfacePool(flowId, interfaceOptions);
        if (variantOptions.Count == 0)
        {
            throw new GirvsException($"流程 '{flowId}' Variants 不能为空");
        }

        var variants = new Dictionary<string, FlowVariantDefinition>(StringComparer.OrdinalIgnoreCase);
        var indexesByInterface = new Dictionary<string, List<FlowVariantStepIndex>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (configuredKey, references) in variantOptions)
        {
            var variantKey = RequireValue(configuredKey, $"流程 '{flowId}' 的组合键不能为空");
            // Dictionary 默认区分大小写，这里额外判重以落实“忽略大小写唯一”的契约。
            if (variants.ContainsKey(variantKey))
            {
                throw new GirvsException($"流程 '{flowId}' 的组合键 '{variantKey}' 重复");
            }

            if (references is null || references.Count < 2)
            {
                throw new GirvsException($"流程 '{flowId}' 的组合 '{variantKey}' 至少需要两个步骤");
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var steps = new List<FlowStepDefinition>(references.Count);
            foreach (var reference in references)
            {
                var name = RequireValue(reference, $"流程 '{flowId}' 的组合 '{variantKey}' 的接口引用不能为空");
                // 引用必须指向接口池内的名称，禁止隐式新建接口。
                if (!interfaces.TryGetValue(name, out var step))
                {
                    throw new GirvsException($"流程 '{flowId}' 的组合 '{variantKey}' 引用了未定义的接口 '{name}'");
                }

                // 同一组合内重复引用同一接口会产生两个顺位相同的入口，语义不成立。
                if (!seen.Add(name))
                {
                    throw new GirvsException($"流程 '{flowId}' 的组合 '{variantKey}' 重复引用接口 '{name}'");
                }

                if (!indexesByInterface.TryGetValue(name, out var indexes))
                {
                    indexes = [];
                    indexesByInterface.Add(name, indexes);
                }

                // 记录该接口在本组合中的顺位（等于此前已追加的步骤数）。
                indexes.Add(new FlowVariantStepIndex(variantKey, steps.Count));
                steps.Add(step);
            }

            variants.Add(variantKey, new FlowVariantDefinition(variantKey, steps.AsReadOnly()));
        }

        var flow = new FlowDefinition(flowId, ttlSeconds, [], FlowDefinitionKind.Parameterized, header, variants.AsReadOnly());
        foreach (var (name, step) in interfaces)
        {
            // 孤立接口永远不会被任何组合命中，属于明显的配置残留，直接拒绝而非静默忽略。
            if (!indexesByInterface.TryGetValue(name, out var indexes))
            {
                throw new GirvsException($"流程 '{flowId}' 的接口 '{name}' 未被任何组合引用");
            }

            // 参数化接口没有线性匹配，顺位完全由票据已锁定的组合索引决定。
            RegisterEndpoint(new FlowEndpointMatch(flow, step, null, indexes.AsReadOnly()));
        }
    }

    /// <summary>
    /// 构建参数化流程的命名接口池，并保证名称唯一、方法+路径唯一。
    /// </summary>
    /// <param name="flowId">流程标识，仅用于错误信息。</param>
    /// <param name="interfaceOptions">接口池配置。</param>
    /// <returns>接口名（忽略大小写）到步骤定义的映射。</returns>
    /// <exception cref="GirvsException">
    /// 接口池为空、包含空项、名称为空或重复、或方法+路径重复。
    /// </exception>
    private static Dictionary<string, FlowStepDefinition> BuildInterfacePool(
        string flowId,
        List<FlowInterfaceOptions> interfaceOptions
    )
    {
        if (interfaceOptions.Count == 0)
        {
            throw new GirvsException($"流程 '{flowId}' Interfaces 不能为空");
        }

        var interfaces = new Dictionary<string, FlowStepDefinition>(StringComparer.OrdinalIgnoreCase);
        var endpointKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in interfaceOptions)
        {
            if (entry is null)
            {
                throw new GirvsException($"流程 '{flowId}' Interfaces 不能包含空项");
            }

            var name = RequireValue(entry.Name, $"流程 '{flowId}' 的接口 Name 不能为空");
            var method = NormalizeMethod(entry.Method, flowId);
            var path = NormalizePath(entry.Path, flowId);
            if (!interfaces.TryAdd(name, new FlowStepDefinition(method, new PathString(path))))
            {
                throw new GirvsException($"流程 '{flowId}' 的接口名称 '{name}' 重复");
            }

            // 接口池内两个名称指向同一方法+路径属于重复定义，无法确定反查结果。
            if (!endpointKeys.Add(CreateKey(method, path)))
            {
                throw new GirvsException($"流程 '{flowId}' 的接口 '{method} {path}' 重复");
            }
        }

        return interfaces;
    }

    /// <summary>
    /// 校验前置参数请求头名，并确保它不与流程防护自身占用的请求头/响应头冲突。
    /// </summary>
    /// <param name="options">全局配置，提供可配置的票据头与业务标识头名称。</param>
    /// <param name="flowId">流程标识，仅用于错误信息。</param>
    /// <param name="value">配置的前置参数请求头名。</param>
    /// <returns>去除首尾空白后的请求头名。</returns>
    /// <exception cref="GirvsException">
    /// 请求头名为空，或与保留头（票据头、业务标识头、<c>X-Flow-Next-Index</c>、
    /// <c>X-Flow-Completed</c>、<c>X-Flow-Business-Id</c>）冲突。
    /// </exception>
    /// <remarks>
    /// 冲突检查忽略大小写并对保留值做 <c>Trim()</c>，因为 HTTP 头名本身不区分大小写，
    /// 若允许复用保留头，同一个值会被同时解读为“前置参数”和“续用凭据”，导致不可预测的放行或拒绝。
    /// </remarks>
    private static string ValidateParameterHeader(FlowProtectionOptions options, string flowId, string value)
    {
        var header = RequireValue(value, $"流程 '{flowId}' ParameterHeaderName 不能为空");
        string?[] reservedHeaders =
        [
            options.TicketHeaderName,
            options.BusinessIdHeaderName,
            FlowProtectionHeaders.NextIndex,
            FlowProtectionHeaders.Completed,
            FlowProtectionHeaders.BusinessIdFromResponse,
        ];
        if (reservedHeaders.Any(reserved => string.Equals(header, reserved?.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new GirvsException($"流程 '{flowId}' ParameterHeaderName '{header}' 与系统保留头冲突");
        }

        return header;
    }

    /// <summary>
    /// 把单个接口归属写入反查表，并保证同一“方法 + 路径”不被多条流程复用。
    /// </summary>
    /// <param name="endpoint">待注册的接口归属信息。</param>
    /// <exception cref="GirvsException">该方法+路径已被其它流程（或同一流程的其它步骤）注册。</exception>
    private void RegisterEndpoint(FlowEndpointMatch endpoint)
    {
        var step = endpoint.Step;
        if (!_endpointsByMethodAndPath.TryAdd(CreateKey(step.Method, step.Path.Value!), endpoint))
        {
            throw new GirvsException($"流程步骤 '{step.Method} {step.Path}' 被多个流程重复使用");
        }
    }

    /// <summary>
    /// 规范化并校验 HTTP 方法，返回大写形式。
    /// </summary>
    /// <param name="method">原始配置值。</param>
    /// <param name="flowId">流程标识，仅用于错误信息。</param>
    /// <returns>大写的方法名。</returns>
    /// <exception cref="GirvsException">方法为空或不在允许列表内。</exception>
    private static string NormalizeMethod(string method, string flowId)
    {
        method = RequireValue(method, $"流程 '{flowId}' Method 不能为空").ToUpperInvariant();
        if (!AllowedMethods.Contains(method))
        {
            throw new GirvsException($"流程 '{flowId}' Method '{method}' 不是合法 HTTP Method");
        }

        return method;
    }

    /// <summary>
    /// 校验并规范化路径：必须以 <c>/</c> 开头，非根路径去除尾斜杠。
    /// </summary>
    /// <param name="path">原始配置值或请求路径。</param>
    /// <param name="flowId">调用方标签（流程标识或固定标记），仅用于错误信息。</param>
    /// <returns>规范化后的路径。</returns>
    /// <exception cref="GirvsException">路径为空或不以 <c>/</c> 开头。</exception>
    /// <remarks>
    /// 运行期反查与启动期注册共用本方法，保证两侧规范化规则完全一致，
    /// 否则会出现“配置看似匹配、实际反查不到”的静默失效。
    /// </remarks>
    private static string NormalizePath(string path, string flowId)
    {
        path = RequireValue(path, $"流程 '{flowId}' Path 不能为空");
        if (!path.StartsWith('/'))
        {
            throw new GirvsException($"流程 '{flowId}' Path 必须以 / 开始");
        }

        return path.Length > 1 ? path.TrimEnd('/') : path;
    }

    /// <summary>
    /// 断言字符串非空白并返回去除首尾空白后的值。
    /// </summary>
    /// <param name="value">待校验的值。</param>
    /// <param name="message">为空时使用的错误消息。</param>
    /// <returns>已去除首尾空白的值。</returns>
    /// <exception cref="GirvsException"><paramref name="value" /> 为 <see langword="null" />、空串或仅空白。</exception>
    private static string RequireValue(string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new GirvsException(message);
        }

        return value.Trim();
    }

    /// <summary>
    /// 校验存活时长取值范围：1 到 1800 秒（含端点）。
    /// </summary>
    /// <param name="name">字段名，用于错误信息定位。</param>
    /// <param name="ttlSeconds">待校验的秒数。</param>
    /// <exception cref="GirvsException">取值越界。</exception>
    /// <remarks>
    /// 上限 1800 秒是为了避免票据长期滞留，把“一次性流程”变成事实上的长期状态。
    /// </remarks>
    private static void ValidateTtl(string name, int ttlSeconds)
    {
        if (ttlSeconds is <= 0 or > 1800)
        {
            throw new GirvsException($"{name} 必须在 1 到 1800 秒之间");
        }
    }

    /// <summary>
    /// 生成反查表键：大写方法名加一个空格与路径。
    /// </summary>
    /// <param name="method">HTTP 方法。</param>
    /// <param name="path">已规范化的路径。</param>
    /// <returns>反查键。</returns>
    /// <remarks>
    /// 方法名统一大写后再拼接，配合忽略大小写的字典，使任意大小写书写都落到同一条目。
    /// </remarks>
    private static string CreateKey(string method, string path) => $"{method.ToUpperInvariant()} {path}";
}
