namespace Girvs.Aspire.Hosting.SettingsProviders;

public static class ProviderEndpointExtensions
{
    /// <summary>取资源主 endpoint 的已分配地址(要求 endpoint 已分配,运行期由 WaitFor 保证)。</summary>
    public static (string Host, int Port) PrimaryEndpoint(this IResourceWithEndpoints resource)
    {
        var endpoint = resource.GetEndpoints().First();
        return (endpoint.Host, endpoint.Port);
    }
}
