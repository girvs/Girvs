using Girvs;
using Girvs.Aspire.Gateway.FlowProtection.Configuration;

namespace Girvs.Aspire.Gateway.FlowProtection;

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

    private readonly Dictionary<string, FlowStepMatch> _stepsByMethodAndPath;

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
        var stepIndex = new Dictionary<string, FlowStepMatch>(StringComparer.OrdinalIgnoreCase);

        foreach (var flowOptions in options.Flows)
        {
            var flowId = RequireValue(flowOptions.FlowId, "FlowId 不能为空");
            if (!flowIds.Add(flowId))
            {
                throw new GirvsException($"FlowId '{flowId}' 重复");
            }

            if (flowOptions.Steps.Count < 2)
            {
                throw new GirvsException($"流程 '{flowId}' 至少需要两个步骤");
            }

            var ttlSeconds = flowOptions.TtlSeconds ?? options.DefaultTtlSeconds;
            ValidateTtl($"流程 '{flowId}' TtlSeconds", ttlSeconds);

            var flowStepKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var steps = new List<FlowStepDefinition>(flowOptions.Steps.Count);
            foreach (var stepOptions in flowOptions.Steps)
            {
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
                var key = CreateKey(step.Method, step.Path.Value!);
                if (stepIndex.ContainsKey(key))
                {
                    throw new GirvsException($"流程步骤 '{step.Method} {step.Path}' 被多个流程重复使用");
                }

                stepIndex.Add(key, new FlowStepMatch(flow, step, i));
            }
        }

        _stepsByMethodAndPath = stepIndex;
    }

    public bool TryGetStep(string method, PathString path, out FlowStepMatch match)
    {
        var normalizedPath = NormalizePath(path.Value ?? string.Empty, "request");
        return _stepsByMethodAndPath.TryGetValue(CreateKey(method, normalizedPath), out match!);
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
