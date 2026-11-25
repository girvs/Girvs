using Girvs;

namespace Girvs.Gateway.FlowProtection;

/// <summary>已校验的下游路径定义，参数只占用一个完整路径段。</summary>
internal sealed class FlowPathTemplate
{
    // 参数段用 null 表示。参数名称只参与配置校验和冲突诊断。
    private readonly string?[]? _segments;

    private FlowPathTemplate(string path, string?[]? segments)
    {
        Path = path;
        _segments = segments;
        SegmentCount = segments?.Length ?? CountSegments(path);
    }

    /// <summary>规范化后的下游路径。</summary>
    internal string Path { get; }

    /// <summary>路径是否包含参数段。</summary>
    internal bool HasParameter => _segments is not null;

    /// <summary>路径段数，根路径为零段。</summary>
    internal int SegmentCount { get; }

    internal static FlowPathTemplate Parse(string configuredPath, string flowId) =>
        Parse(configuredPath, flowId, out var path) ?? new FlowPathTemplate(path, null);

    /// <summary>校验并规范化配置路径。仅参数路径需要分配编译后的模板。</summary>
    internal static FlowPathTemplate? Parse(string configuredPath, string flowId, out string path)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            throw new GirvsException($"流程 '{flowId}' Path 不能为空");
        }

        path = configuredPath.Trim();
        if (!path.StartsWith('/'))
        {
            throw new GirvsException($"流程 '{flowId}' Path 必须以 / 开始");
        }

        if (path.Contains("//", StringComparison.Ordinal))
        {
            throw new GirvsException($"流程 '{flowId}' Path '{path}' 包含空路径段");
        }

        if (path.AsSpan().IndexOfAny('*', '?', '#') >= 0)
        {
            throw new GirvsException($"流程 '{flowId}' Path '{path}' 包含非法字符（*、?、#）");
        }

        var normalizedPath = NormalizeRequestPath(path);
        if (normalizedPath.Length != path.Length)
        {
            path = normalizedPath.ToString();
        }

        if (path.AsSpan().IndexOfAny('{', '}') < 0)
        {
            return null;
        }

        var segments = new string?[CountSegments(path)];
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var index = 0;
        var pathSegments = path.AsSpan(1);
        foreach (var range in pathSegments.Split('/'))
        {
            var segment = pathSegments[range];
            if (segment.IndexOfAny('{', '}') < 0)
            {
                segments[index++] = segment.ToString();
                continue;
            }

            if (segment.Length < 3 || segment[0] != '{' || segment[^1] != '}' || !IsParameterName(segment[1..^1]))
            {
                throw new GirvsException($"流程 '{flowId}' Path '{path}' 包含非法参数段 '{segment}'");
            }

            var name = segment[1..^1].ToString();
            if (!names.Add(name))
            {
                throw new GirvsException($"流程 '{flowId}' Path '{path}' 的参数名 '{name}' 重复");
            }

            index++;
        }

        return new FlowPathTemplate(path, segments);
    }

    /// <summary>判断规范化后的请求路径是否匹配此路径定义。</summary>
    internal bool Matches(ReadOnlySpan<char> normalizedRequestPath)
    {
        if (_segments is null)
        {
            return normalizedRequestPath.Equals(Path, StringComparison.OrdinalIgnoreCase);
        }

        if (normalizedRequestPath.Length < 2 || normalizedRequestPath[0] != '/')
        {
            return false;
        }

        var index = 0;
        var pathSegments = normalizedRequestPath[1..];
        foreach (var range in pathSegments.Split('/'))
        {
            var segment = pathSegments[range];
            if (segment.IsEmpty || index >= _segments.Length)
            {
                return false;
            }

            var literal = _segments[index++];
            if (literal is not null && !segment.Equals(literal, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return index == _segments.Length;
    }

    /// <summary>返回模板冲突的类别和诊断详情。存在字面区分位时返回 null。</summary>
    internal string? DescribeConflict(FlowPathTemplate other)
    {
        if (_segments is null || other._segments is null || SegmentCount != other.SegmentCount)
        {
            return null;
        }

        var sameShape = true;
        for (var i = 0; i < _segments.Length; i++)
        {
            var left = _segments[i];
            var right = other._segments[i];
            if (left is not null && right is not null && !string.Equals(left, right, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            sameShape &= (left is null) == (right is null);
        }

        if (sameShape)
        {
            return string.Equals(Path, other.Path, StringComparison.OrdinalIgnoreCase) ? "重复" : "同形异名冲突";
        }

        var witness = new string[_segments.Length];
        for (var i = 0; i < witness.Length; i++)
        {
            witness[i] = _segments[i] ?? other._segments[i] ?? "value";
        }

        return $"交叉重叠冲突，见证路径 '/{string.Join('/', witness)}'";
    }

    /// <summary>移除非根路径的一个尾斜杠，返回原路径的切片。</summary>
    internal static ReadOnlySpan<char> NormalizeRequestPath(ReadOnlySpan<char> requestPath) =>
        requestPath.Length > 1 && requestPath[^1] == '/' ? requestPath[..^1] : requestPath;

    /// <summary>计算规范化路径的段数，根路径为零段。</summary>
    internal static int CountSegments(ReadOnlySpan<char> path) => path.Length <= 1 ? 0 : path.Count('/');

    private static bool IsParameterName(ReadOnlySpan<char> name)
    {
        if (!char.IsAsciiLetter(name[0]) && name[0] != '_')
        {
            return false;
        }

        foreach (var character in name[1..])
        {
            if (!char.IsAsciiLetterOrDigit(character) && character != '_')
            {
                return false;
            }
        }

        return true;
    }
}
