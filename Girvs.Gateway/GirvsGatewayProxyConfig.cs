namespace Girvs.Gateway;

/// <summary>Girvs 网关的一份代理配置快照：路由 + 集群 + 变更令牌。</summary>
public sealed class GirvsGatewayProxyConfig(
    IReadOnlyList<RouteConfig> routes,
    IReadOnlyList<ClusterConfig> clusters,
    IChangeToken changeToken
) : IProxyConfig
{
    public IReadOnlyList<RouteConfig> Routes { get; } = routes;
    public IReadOnlyList<ClusterConfig> Clusters { get; } = clusters;
    public IChangeToken ChangeToken { get; } = changeToken;
}
