using Girvs.Gateway.Discovery;

namespace Sample.Gateway;

public sealed class SwaggerFilterMiddleware(
    RequestDelegate next,
    SwaggerEndpointEnumerator swaggerEndpoints
)
{
    public Task InvokeAsync(HttpContext context, IGatewayServiceDiscoverySource source)
    {
        swaggerEndpoints.Refresh(source);
        return next(context);
    }
}
