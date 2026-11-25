using Yarp.ReverseProxy.Model;

namespace Girvs.Gateway.FlowProtection;

/// <summary>
/// 把网关收到的客户端请求路径还原为下游服务路径，供流程防护按配置中的下游路径反查流程步骤。
/// </summary>
/// <remarks>
/// <para>
/// 网关采用约定路由：客户端请求 <c>/{服务名}/...</c>，转发时通过 <c>PathRemovePrefix</c>
/// 剥掉服务名前缀。流程配置书写的是下游路径（如 <c>/api/pay1</c>），因此必须先做同一套剥离，
/// 才能与配置精确匹配。
/// </para>
/// <para>
/// 该类型无状态、可安全地被多线程并发调用。
/// </para>
/// </remarks>
public sealed class DownstreamPathResolver
{
    /// <summary>
    /// 计算当前请求对应的下游服务路径。
    /// </summary>
    /// <param name="context">当前请求上下文，须已由 YARP 选出路由（含转换配置）。</param>
    /// <returns>
    /// 成功时返回以 <c>/</c> 开头、且不以 <c>/</c> 结尾的下游路径（服务根路径返回 <c>/</c>）；
    /// 无法唯一确定剥离前缀时返回 <see langword="null" />，表示配置无法支撑流程反查。
    /// </returns>
    /// <remarks>
    /// 只有在“恰好一个有效 <c>PathRemovePrefix</c> 且请求路径确实以该前缀整段开头”时才返回结果。
    /// 任一条件不成立都返回 <see langword="null" />，由调用方按配置错误处理，绝不猜测前缀，
    /// 以免把配置疏漏掩盖成运行期的错误放行或错误拒绝。
    /// </remarks>
    public PathString? Resolve(HttpContext context)
    {
        // YARP 在路由选择阶段写入该特性；尚未选路（或未启用反向代理）时无法判断，直接判为不可解析。
        var feature = context.Features.Get<IReverseProxyFeature>();
        var transforms = feature?.Route?.Config?.Transforms;
        if (transforms is null)
        {
            return null;
        }

        // 收集全部有效剥前缀声明；空值视为未配置，避免产生空前缀导致整条路径被误剥。
        var prefixes = transforms
            .Where(x => x.TryGetValue("PathRemovePrefix", out _))
            .Select(x => x["PathRemovePrefix"])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim('/'))
            .ToArray();

        // 唯一前缀判定：0 个（未配置）或多个（含义歧义）都无法得到确定的下游路径。
        if (prefixes.Length != 1)
        {
            return null;
        }

        var requestPath = context.Request.Path.Value ?? string.Empty;
        var prefixPath = "/" + prefixes[0];
        if (!requestPath.StartsWith(prefixPath, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // 必须整段匹配前缀：/orders 不能把 /orders-archive/x 当作自身前缀剥掉。
        if (requestPath.Length > prefixPath.Length && requestPath[prefixPath.Length] != '/')
        {
            return null;
        }

        // 剩余部分即下游路径；请求恰为服务根地址时剩余为空串，统一归一为 "/"。
        var downstreamPath = requestPath[prefixPath.Length..];
        return string.IsNullOrEmpty(downstreamPath)
            ? new PathString("/")
            : new PathString(downstreamPath);
    }
}
