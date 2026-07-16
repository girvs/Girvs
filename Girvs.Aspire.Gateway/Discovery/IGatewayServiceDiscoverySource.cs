namespace Girvs.Aspire.Gateway.Discovery;

/// <summary>网关服务发现来源：产出当前服务快照，并在服务集变化时触发 ServicesChanged。</summary>
public interface IGatewayServiceDiscoverySource
{
    /// <summary>当前已发现的服务快照。</summary>
    IReadOnlyList<GatewayServiceEndpoint> GetServices();

    /// <summary>服务集发生变化（增删改）时触发。</summary>
    event Action ServicesChanged;

    /// <summary>启动发现（轮询定时器或 watch 连接）。</summary>
    Task StartAsync(CancellationToken cancellationToken);
}
