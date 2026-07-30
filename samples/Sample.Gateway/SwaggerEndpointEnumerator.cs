using Girvs.ServiceGovernance.Discovery;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Sample.Gateway;

public sealed class SwaggerEndpointEnumerator : List<UrlDescriptor>
{
    public void Refresh(IServiceDirectory directory)
    {
        Clear();

        AddRange(
            directory
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
