namespace Girvs.ServiceGovernance.Discovery;

internal sealed class ServiceDirectoryHealthCheck(
    IServiceDirectoryHealth directoryHealth,
    ServiceGovernanceConfig config
) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        var stale = directoryHealth.IsStale(TimeSpan.FromSeconds(Math.Max(1, config.MaxStaleDuration)));
        return Task.FromResult(
            stale
                ? HealthCheckResult.Unhealthy("服务目录尚未成功刷新或快照已过期")
                : HealthCheckResult.Healthy("服务目录快照可用")
        );
    }
}
