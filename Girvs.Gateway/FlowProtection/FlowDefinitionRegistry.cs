using Girvs;
using Girvs.Gateway.FlowProtection.Configuration;

namespace Girvs.Gateway.FlowProtection;

public sealed class FlowDefinitionRegistry
{
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

    private readonly Dictionary<string, FlowEndpointMatch> _endpointsByMethodAndPath =
        new(StringComparer.OrdinalIgnoreCase);

    public FlowDefinitionRegistry(FlowProtectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        ValidateTtl(nameof(options.DefaultTtlSeconds), options.DefaultTtlSeconds);
        ValidateTtl(nameof(options.CompletedTtlSeconds), options.CompletedTtlSeconds);
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

            var flowId = RequireValue(flowOptions.FlowId, "FlowId 不能为空");
            if (!flowIds.Add(flowId))
            {
                throw new GirvsException($"FlowId '{flowId}' 重复");
            }

            // 配置绑定会把显式 null 覆盖到集合属性上，这里统一按空集合处理。
            var stepOptionsList = flowOptions.Steps ?? [];
            var interfaces = flowOptions.Interfaces ?? [];
            var variants = flowOptions.Variants ?? [];
            var hasLinear = stepOptionsList.Count > 0;
            var hasParameterized = !string.IsNullOrWhiteSpace(flowOptions.ParameterHeaderName)
                || interfaces.Count > 0 || variants.Count > 0;
            if (hasLinear && hasParameterized)
            {
                throw new GirvsException($"流程 '{flowId}' 不能同时配置 Steps 和前置参数组合");
            }

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
    public bool TryGetStep(string method, PathString path, out FlowStepMatch match)
    {
        match = TryGetEndpoint(method, path, out var endpoint) ? endpoint.LinearMatch! : null!;
        return match is not null;
    }

    public bool TryGetEndpoint(string method, PathString path, out FlowEndpointMatch match)
    {
        var normalizedPath = NormalizePath(path.Value ?? string.Empty, "request");
        return _endpointsByMethodAndPath.TryGetValue(CreateKey(method, normalizedPath), out match!);
    }

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
            RegisterEndpoint(new FlowEndpointMatch(flow, step, new FlowStepMatch(flow, step, i), []));
        }
    }

    private void RegisterParameterized(
        string flowId,
        int ttlSeconds,
        string header,
        List<FlowInterfaceOptions> interfaceOptions,
        Dictionary<string, List<string>> variantOptions
    )
    {
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
                if (!interfaces.TryGetValue(name, out var step))
                {
                    throw new GirvsException($"流程 '{flowId}' 的组合 '{variantKey}' 引用了未定义的接口 '{name}'");
                }

                if (!seen.Add(name))
                {
                    throw new GirvsException($"流程 '{flowId}' 的组合 '{variantKey}' 重复引用接口 '{name}'");
                }

                if (!indexesByInterface.TryGetValue(name, out var indexes))
                {
                    indexes = [];
                    indexesByInterface.Add(name, indexes);
                }

                indexes.Add(new FlowVariantStepIndex(variantKey, steps.Count));
                steps.Add(step);
            }

            variants.Add(variantKey, new FlowVariantDefinition(variantKey, steps.AsReadOnly()));
        }

        var flow = new FlowDefinition(flowId, ttlSeconds, [], FlowDefinitionKind.Parameterized, header, variants.AsReadOnly());
        foreach (var (name, step) in interfaces)
        {
            if (!indexesByInterface.TryGetValue(name, out var indexes))
            {
                throw new GirvsException($"流程 '{flowId}' 的接口 '{name}' 未被任何组合引用");
            }

            RegisterEndpoint(new FlowEndpointMatch(flow, step, null, indexes.AsReadOnly()));
        }
    }

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

            if (!endpointKeys.Add(CreateKey(method, path)))
            {
                throw new GirvsException($"流程 '{flowId}' 的接口 '{method} {path}' 重复");
            }
        }

        return interfaces;
    }

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

    private void RegisterEndpoint(FlowEndpointMatch endpoint)
    {
        var step = endpoint.Step;
        if (!_endpointsByMethodAndPath.TryAdd(CreateKey(step.Method, step.Path.Value!), endpoint))
        {
            throw new GirvsException($"流程步骤 '{step.Method} {step.Path}' 被多个流程重复使用");
        }
    }

    private static string NormalizeMethod(string method, string flowId)
    {
        method = RequireValue(method, $"流程 '{flowId}' Method 不能为空").ToUpperInvariant();
        if (!AllowedMethods.Contains(method))
        {
            throw new GirvsException($"流程 '{flowId}' Method '{method}' 不是合法 HTTP Method");
        }

        return method;
    }

    private static string NormalizePath(string path, string flowId)
    {
        path = RequireValue(path, $"流程 '{flowId}' Path 不能为空");
        if (!path.StartsWith('/'))
        {
            throw new GirvsException($"流程 '{flowId}' Path 必须以 / 开始");
        }

        return path.Length > 1 ? path.TrimEnd('/') : path;
    }

    private static string RequireValue(string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new GirvsException(message);
        }

        return value.Trim();
    }

    private static void ValidateTtl(string name, int ttlSeconds)
    {
        if (ttlSeconds is <= 0 or > 1800)
        {
            throw new GirvsException($"{name} 必须在 1 到 1800 秒之间");
        }
    }

    private static string CreateKey(string method, string path) => $"{method.ToUpperInvariant()} {path}";
}
