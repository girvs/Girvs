namespace Girvs.Gateway;

/// <summary>
/// Girvs 网关的一份代理配置快照：路由 + 集群 + 变更令牌。
/// </summary>
/// <remarks>
/// 该快照是不可变的：每次服务目录变化都会构建一个全新实例并在
/// <see cref="GirvsGatewayProxyConfigProvider"/> 中原子替换，旧快照随后通过其
/// <see cref="ChangeToken"/> 取消通知 YARP 重新拉取配置。因此同一实例可被多个
/// 请求线程并发读取，无需额外同步。
/// </remarks>
/// <param name="routes">由服务目录快照生成的 YARP 路由集合（只读）。</param>
/// <param name="clusters">与路由一一对应的 YARP 集群集合（只读）。</param>
/// <param name="changeToken">本快照的失效令牌；令牌触发即表示应重新调用 <c>GetConfig</c> 获取新快照。</param>
public sealed class GirvsGatewayProxyConfig(
    IReadOnlyList<RouteConfig> routes,
    IReadOnlyList<ClusterConfig> clusters,
    IChangeToken changeToken
) : IProxyConfig
{
    /// <summary>获取本快照包含的路由集合。</summary>
    public IReadOnlyList<RouteConfig> Routes { get; } = routes;

    /// <summary>获取本快照包含的集群集合。</summary>
    public IReadOnlyList<ClusterConfig> Clusters { get; } = clusters;

    /// <summary>获取本快照对应的变更令牌，用于向 YARP 通知配置失效。</summary>
    public IChangeToken ChangeToken { get; } = changeToken;
}
