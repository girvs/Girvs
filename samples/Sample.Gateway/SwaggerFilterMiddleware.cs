using Girvs.ServiceGovernance.Discovery;

namespace Sample.Gateway;

public sealed class SwaggerFilterMiddleware(
    RequestDelegate next,
    SwaggerEndpointEnumerator swaggerEndpoints
)
{
    public Task InvokeAsync(HttpContext context, IServiceDirectory directory)
    {
        swaggerEndpoints.Refresh(directory);
        return next(context);
    }
}
