namespace Girvs.Refit.HttpClientHandlers;

public class AuthenticatedHttpClientHandler(
    RefitServiceAttribute refitServiceAttribute,
    IEnumerable<IRefitServiceEndpointResolver> resolvers,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AuthenticatedHttpClientHandler> logger) : DelegatingHandler
{
    private static readonly HashSet<string> ExcludedHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Connection", "Keep-Alive", "Proxy-Authenticate", "Proxy-Authorization", "TE", "Trailer",
        "Transfer-Encoding", "Upgrade", "Host", "Content-Length"
    };

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (refitServiceAttribute.AddressType == RefitServiceAddressType.ServiceDiscovery)
            CopyRequestHeaders(request, httpContextAccessor.HttpContext);

        var resolver = resolvers.Single(x => x.CanResolve(refitServiceAttribute.AddressType));
        var endpoint = await resolver.ResolveAsync(refitServiceAttribute.ServiceName, cancellationToken);
        if (endpoint is not null)
        {
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
