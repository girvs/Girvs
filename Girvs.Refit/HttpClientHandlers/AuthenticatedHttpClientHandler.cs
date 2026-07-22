namespace Girvs.Refit.HttpClientHandlers;

public class AuthenticatedHttpClientHandler(
    RefitServiceAttribute refitServiceAttribute,
    IEnumerable<IRefitServiceEndpointResolver> resolvers,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AuthenticatedHttpClientHandler> logger) : DelegatingHandler
{
    // 这些头属于连接级语义或由 HttpClient 管理，转发会导致下游请求无效。
    private static readonly HashSet<string> ExcludedHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Connection", "Keep-Alive", "Proxy-Authenticate", "Proxy-Authorization", "TE", "Trailer",
        "Transfer-Encoding", "Upgrade", "Host", "Content-Length"
    };

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // 仅内部服务调用转发当前身份、租户和链路上下文；避免泄漏到第三方静态地址。
        if (refitServiceAttribute.AddressType == RefitServiceAddressType.ServiceDiscovery)
            CopyRequestHeaders(request, httpContextAccessor.HttpContext);

        // 地址来源决定解析器，发现提供者在模块启动时已由配置固定。
        var resolver = resolvers.Single(x => x.CanResolve(refitServiceAttribute.AddressType));
        var endpoint = await resolver.ResolveAsync(refitServiceAttribute.ServiceName, cancellationToken);
        if (endpoint is not null)
        {
            // 保留 Refit 生成的路径和查询参数，只替换目标服务的主机部分。
            var builder = new UriBuilder(request.RequestUri)
            {
                Scheme = endpoint.Scheme,
                Host = endpoint.Host,
                Port = endpoint.IsDefaultPort ? -1 : endpoint.Port
            };
            request.RequestUri = builder.Uri;
        }

        logger.LogInformation("Refit 请求服务 {ServiceName}，地址来源 {AddressType}，请求地址 {RequestUri}",
            refitServiceAttribute.ServiceName, refitServiceAttribute.AddressType, request.RequestUri);
        return await base.SendAsync(request, cancellationToken);
    }

    private static void CopyRequestHeaders(HttpRequestMessage request, HttpContext context)
    {
        if (context is null) return;
        foreach (var header in context.Request.Headers)
        {
            if (!ExcludedHeaders.Contains(header.Key))
                request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }
    }
}
