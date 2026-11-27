using Girvs;
using Girvs.Gateway.FlowProtection.Configuration;

namespace Girvs.Gateway.FlowProtection;

/// <summary>
/// 流程定义注册表：在启动期校验配置、编译为只读流程定义，并提供“方法 + 下游路径”到接口归属的确定性反查。
/// </summary>
/// <remarks>
/// <para>
/// 本类型承担两类职责：一是把配置中的接口池与流程组合
/// 校验并翻译成运行期使用的 <see cref="FlowDefinition"/> / <see cref="FlowEndpointMatch"/>；
/// 二是在运行期优先按静态路径反查，未命中时按方法和段数匹配路径模板。
/// </para>
/// <para>
/// 构造完成后内部集合不再变更，因此可安全地注册为单例并被所有请求线程并发读取。
/// 校验失败一律抛 <see cref="GirvsException"/>，把问题暴露在启动期而不是运行期。
/// </para>
/// <para>
/// 同一 HTTP 方法下静态路径唯一，路径模板的匹配范围互不重叠。
/// 同一组合内步骤不得重复，流程中每个接口至少被一条组合引用。
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

    private readonly Dictionary<(string Method, int SegmentCount), TemplateEndpoint[]>? _templatesByMethodAndSegmentCount;

    /// <summary>
    /// 校验并编译流程防护配置。
    /// </summary>
    /// <param name="options">流程防护配置。</param>
    /// <exception cref="ArgumentNullException"><paramref name="options" /> 为 <see langword="null" />。</exception>
    /// <exception cref="GirvsException">
    /// 任一校验规则不满足：存活时长越界、处理锁时长非法、流程标识重复、
    /// 接口池或组合无效、接口未被引用、路径或方法非法、接口被多条流程复用等。
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
        TemplateRegistration? templateRegistration = null;
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
            var interfaces = flowOptions.Interfaces ?? [];
            var variants = flowOptions.Variants ?? [];

            // 单条流程未指定存活时长时回退到全局默认值。
            var ttlSeconds = flowOptions.TtlSeconds ?? options.DefaultTtlSeconds;
            ValidateTtl($"流程 '{flowId}' TtlSeconds", ttlSeconds);
            var header = ValidateParameterHeader(options, flowId, flowOptions.ParameterHeaderName, variants.Count);
            RegisterFlow(flowId, ttlSeconds, header, interfaces, variants, ref templateRegistration);
        }

        _templatesByMethodAndSegmentCount = templateRegistration?.BuildLookup();
    }

    /// <summary>
    /// 按请求的方法与下游路径反查接口所属流程及候选组合。
    /// </summary>
    /// <param name="method">请求的 HTTP 方法，比较时忽略大小写。</param>
    /// <param name="path">还原后的下游服务路径，非根路径移除一个尾斜杠后参与匹配。</param>
    /// <param name="match">命中时返回接口归属信息；否则为 <see langword="null" />。</param>
    /// <returns>该接口受流程防护约束时返回 <see langword="true" />；未配置保护时返回 <see langword="false" />，调用方应直接放行。</returns>
    /// <remarks>
    /// 静态路径优先于模板。模板参数匹配任意非空路径段。无效的请求方法或路径返回未命中。
    /// </remarks>
    public bool TryGetEndpoint(string method, PathString path, out FlowEndpointMatch match)
    {
        match = null!;
        var requestPath = path.Value.AsSpan();
        if (requestPath.IsEmpty || requestPath[0] != '/' || !AllowedMethods.TryGetValue(method, out var canonicalMethod))
        {
            return false;
        }

        requestPath = FlowPathTemplate.NormalizeRequestPath(requestPath);

        if (_endpointsByMethodAndPath.TryGetValue(CreateKey(canonicalMethod, requestPath), out match!))
        {
            return true;
        }

        if (_templatesByMethodAndSegmentCount is null)
        {
            return false;
        }

        var bucketKey = (canonicalMethod, FlowPathTemplate.CountSegments(requestPath));
        if (!_templatesByMethodAndSegmentCount.TryGetValue(bucketKey, out var templates))
        {
            return false;
        }

        foreach (var candidate in templates)
        {
            if (candidate.Template.Matches(requestPath))
            {
                match = candidate.Endpoint;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 校验并注册一条流程：构建接口池、编译各流程组合，并为每个接口建立组合顺位索引。
    /// </summary>
    /// <param name="flowId">流程标识。</param>
    /// <param name="ttlSeconds">该流程票据的存活时长（秒）。</param>
    /// <param name="header">已校验的前置参数请求头名；单组合可为空字符串。</param>
    /// <param name="interfaceOptions">命名接口池配置。</param>
    /// <param name="variantOptions">组合键到接口引用列表的映射。</param>
    /// <param name="templateRegistration">构造期全局模板索引，有模板时才创建。</param>
    /// <exception cref="GirvsException">
    /// 组合为空、组合键重复或为空、组合没有步骤、引用未定义或重复引用接口、
    /// 或存在未被任何组合引用的孤立接口。
    /// </exception>
    /// <remarks>
    /// 组合内步骤顺序即接口引用顺序；同一接口可在不同组合中处于不同顺位，
    /// 因此这里为每个接口累积“组合键 → 顺位”的全部候选，供运行期按票据锁定组合精确还原顺位。
    /// </remarks>
    private void RegisterFlow(
        string flowId,
        int ttlSeconds,
        string header,
        List<FlowInterfaceOptions> interfaceOptions,
        Dictionary<string, List<string>> variantOptions,
        ref TemplateRegistration? templateRegistration
    )
    {
        // 先建接口池：后续组合只能引用池内已定义且唯一的方法+路径。
        var interfaces = BuildInterfacePool(flowId, interfaceOptions, out var templates);
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

            if (references is null || references.Count == 0)
            {
                throw new GirvsException($"流程 '{flowId}' 的组合 '{variantKey}' 至少需要一个步骤");
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

        var flow = new FlowDefinition(flowId, ttlSeconds, header, variants.AsReadOnly());
        foreach (var (name, step) in interfaces)
        {
            // 孤立接口永远不会被任何组合命中，属于明显的配置残留，直接拒绝而非静默忽略。
            if (!indexesByInterface.TryGetValue(name, out var indexes))
            {
                throw new GirvsException($"流程 '{flowId}' 的接口 '{name}' 未被任何组合引用");
            }

            RegisterEndpoint(
                new FlowEndpointMatch(flow, step, indexes.AsReadOnly()),
                templates?.GetValueOrDefault(step),
                ref templateRegistration
            );
        }
    }

    /// <summary>
    /// 构建流程的命名接口池，校验名称、静态路径和模板冲突。
    /// </summary>
    /// <param name="flowId">流程标识，仅用于错误信息。</param>
    /// <param name="interfaceOptions">接口池配置。</param>
    /// <param name="templates">有参数的接口到已编译模板的映射。</param>
    /// <returns>接口名（忽略大小写）到步骤定义的映射。</returns>
    /// <exception cref="GirvsException">
    /// 接口池为空、包含空项、名称为空或重复、方法与路径的组合重复、
    /// 方法或路径非法（含非法路径模板），或接口池内存在路径模板冲突。
    /// </exception>
    private static Dictionary<string, FlowStepDefinition> BuildInterfacePool(
        string flowId,
        List<FlowInterfaceOptions> interfaceOptions,
        out Dictionary<FlowStepDefinition, FlowPathTemplate>? templates
    )
    {
        if (interfaceOptions.Count == 0)
        {
            throw new GirvsException($"流程 '{flowId}' Interfaces 不能为空");
        }

        var interfaces = new Dictionary<string, FlowStepDefinition>(StringComparer.OrdinalIgnoreCase);
        var endpointKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        templates = null;
        foreach (var entry in interfaceOptions)
        {
            if (entry is null)
            {
                throw new GirvsException($"流程 '{flowId}' Interfaces 不能包含空项");
            }

            var name = RequireValue(entry.Name, $"流程 '{flowId}' 的接口 Name 不能为空");
            var method = NormalizeMethod(entry.Method, flowId);
            var template = FlowPathTemplate.Parse(entry.Path, flowId, out var path);
            var step = new FlowStepDefinition(method, new PathString(path));
            if (!interfaces.TryAdd(name, step))
            {
                throw new GirvsException($"流程 '{flowId}' 的接口名称 '{name}' 重复");
            }

            if (template is not null)
            {
                RegisterLocalTemplate(ref templates, step, template, $"流程 '{flowId}' 的接口池");
            }
            else if (!endpointKeys.Add(CreateKey(method, path)))
            {
                throw new GirvsException($"流程 '{flowId}' 的接口 '{method} {path}' 重复");
            }
        }

        return interfaces;
    }

    private static void RegisterLocalTemplate(
        ref Dictionary<FlowStepDefinition, FlowPathTemplate>? templates,
        FlowStepDefinition step,
        FlowPathTemplate template,
        string scope
    )
    {
        templates ??= new();
        foreach (var (existingStep, existingTemplate) in templates)
        {
            if (step.Method == existingStep.Method && existingTemplate.DescribeConflict(template) is { } conflict)
            {
                throw new GirvsException($"{scope}的路径模板 '{step.Method} {existingTemplate.Path}' 与 '{step.Method} {template.Path}' {conflict}");
            }
        }

        templates.Add(step, template);
    }

    /// <summary>
    /// 校验前置参数请求头名，并确保它不与流程防护自身占用的请求头/响应头冲突。
    /// </summary>
    /// <param name="options">全局配置，提供可配置的票据头与业务标识头名称。</param>
    /// <param name="flowId">流程标识，仅用于错误信息。</param>
    /// <param name="value">配置的前置参数请求头名。</param>
    /// <param name="variantCount">配置的组合数。</param>
    /// <returns>去除首尾空白后的请求头名；省略时为空字符串。</returns>
    /// <exception cref="GirvsException">
    /// 多组合流程的请求头名为空，或与保留头（票据头、业务标识头、<c>X-Flow-Next-Index</c>、
    /// <c>X-Flow-Completed</c>、<c>X-Flow-Business-Id</c>）冲突。
    /// </exception>
    /// <remarks>
    /// 冲突检查忽略大小写并对保留值做 <c>Trim()</c>，因为 HTTP 头名本身不区分大小写，
    /// 若允许复用保留头，同一个值会被同时解读为“前置参数”和“续用凭据”，导致不可预测的放行或拒绝。
    /// </remarks>
    private static string ValidateParameterHeader(FlowProtectionOptions options, string flowId, string? value, int variantCount)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (variantCount >= 2)
            {
                throw new GirvsException($"流程 '{flowId}' ParameterHeaderName 不能为空");
            }

            return string.Empty;
        }

        var header = value.Trim();
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
    /// 把接口写入静态反查表或模板索引，并校验跨流程的路径冲突。
    /// </summary>
    /// <param name="endpoint">待注册的接口归属信息。</param>
    /// <param name="template">参数路径对应的已编译模板。</param>
    /// <param name="templateRegistration">构造期全局模板索引，有模板时才创建。</param>
    /// <exception cref="GirvsException">静态路径重复使用或模板匹配范围重叠。</exception>
    private void RegisterEndpoint(
        FlowEndpointMatch endpoint,
        FlowPathTemplate? template,
        ref TemplateRegistration? templateRegistration
    )
    {
        var step = endpoint.Step;
        if (template is null)
        {
            if (!_endpointsByMethodAndPath.TryAdd(CreateKey(step.Method, step.Path.Value), endpoint))
            {
                throw new GirvsException($"流程步骤 '{step.Method} {step.Path}' 被多个流程重复使用");
            }

            return;
        }

        templateRegistration ??= new();
        var bucket = templateRegistration.GetBucket(step.Method, template.SegmentCount);
        foreach (var existing in bucket)
        {
            if (!ReferenceEquals(existing.Endpoint.Flow, endpoint.Flow) && existing.Template.DescribeConflict(template) is { } conflict)
            {
                throw new GirvsException(
                    $"跨流程路径模板冲突：流程 '{existing.Endpoint.Flow.FlowId}' 的 '{step.Method} {existing.Template.Path}' "
                    + $"与流程 '{endpoint.Flow.FlowId}' 的 '{step.Method} {template.Path}' {conflict}");
            }
        }

        bucket.Add(new TemplateEndpoint(template, endpoint));
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
    /// 生成忽略大小写的静态反查键，由 HTTP 方法、空格和路径组成。
    /// </summary>
    /// <param name="method">HTTP 方法。</param>
    /// <param name="path">已规范化的路径。</param>
    /// <returns>反查键。</returns>
    private static string CreateKey(string method, ReadOnlySpan<char> path) => string.Concat(method, " ", path);

    private readonly record struct TemplateEndpoint(FlowPathTemplate Template, FlowEndpointMatch Endpoint);

    private sealed class TemplateRegistration
    {
        private readonly Dictionary<(string Method, int SegmentCount), List<TemplateEndpoint>> _buckets = new();

        internal List<TemplateEndpoint> GetBucket(string method, int segmentCount)
        {
            var key = (method, segmentCount);
            if (!_buckets.TryGetValue(key, out var bucket))
            {
                bucket = [];
                _buckets.Add(key, bucket);
            }

            return bucket;
        }

        internal Dictionary<(string Method, int SegmentCount), TemplateEndpoint[]> BuildLookup() =>
            _buckets.ToDictionary(bucket => bucket.Key, bucket => bucket.Value.ToArray());
    }
}
