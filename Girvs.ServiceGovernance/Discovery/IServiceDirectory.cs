namespace Girvs.ServiceGovernance.Discovery;

/// <summary>当前可路由业务服务的只读本地目录。</summary>
public interface IServiceDirectory
{
    IReadOnlyList<DiscoveredService> GetServices();

    DiscoveredService? GetService(string serviceName);

    event Action? ServicesChanged;
}

/// <summary>服务目录的刷新状态，供 readiness 健康检查使用。</summary>
public interface IServiceDirectoryHealth
{
    bool HasLoaded { get; }

    DateTimeOffset? LastSuccessUtc { get; }

    bool IsStale(TimeSpan maxStaleDuration);
}
