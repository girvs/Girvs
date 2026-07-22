using Girvs.Gateway.Discovery;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Sample.Gateway;

public sealed class SwaggerEndpointEnumerator : List<UrlDescriptor>
{
    public void Refresh(IGatewayServiceDiscoverySource source)
    {
        Clear();

        AddRange(
            source
                .GetServices()
                .OrderBy(service => service.ServiceName, StringComparer.OrdinalIgnoreCase)
                .Select(service => new UrlDescriptor
                {
                    Url = $"/{service.ServiceName}/girvs_openapi/girvs_api.json",
                    Name = $"{service.ServiceName} API",
                })
        );
    }
}
