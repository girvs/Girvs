using Yarp.ReverseProxy.Model;

namespace Girvs.Aspire.Gateway.FlowProtection;

public sealed class DownstreamPathResolver
{
    public PathString? Resolve(HttpContext context)
    {
        var feature = context.Features.Get<IReverseProxyFeature>();
        var transforms = feature?.Route?.Config?.Transforms;
        if (transforms is null)
        {
            return null;
        }

        var prefixes = transforms
            .Where(x => x.TryGetValue("PathRemovePrefix", out _))
            .Select(x => x["PathRemovePrefix"])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim('/'))
            .ToArray();

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

        if (requestPath.Length > prefixPath.Length && requestPath[prefixPath.Length] != '/')
        {
            return null;
        }

        var downstreamPath = requestPath[prefixPath.Length..];
        return string.IsNullOrEmpty(downstreamPath)
            ? new PathString("/")
            : new PathString(downstreamPath);
    }
}
