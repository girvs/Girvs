using Girvs.Gateway.Discovery;

namespace Sample.Gateway;

public sealed class SwaggerFilterMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SwaggerEndpointEnumerator _swaggerEndpoints;

    public SwaggerFilterMiddleware(
        RequestDelegate next,
        SwaggerEndpointEnumerator swaggerEndpoints)
    {
        _next = next;
        _swaggerEndpoints = swaggerEndpoints;
    }

    public Task InvokeAsync(
        HttpContext context,
        IGatewayServiceDiscoverySource source)
    {
        _swaggerEndpoints.Refresh(source);
        return _next(context);
    }
}
