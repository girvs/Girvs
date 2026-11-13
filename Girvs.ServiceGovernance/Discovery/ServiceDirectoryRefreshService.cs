namespace Girvs.ServiceGovernance.Discovery;

internal sealed class ServiceDirectoryRefreshService(
    IServiceDirectoryProvider provider,
    ServiceDirectory directory,
    ServiceGovernanceConfig config,
    ILogger<ServiceDirectoryRefreshService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(1, config.DiscoveryRefreshInterval));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                directory.Publish(await provider.GetServicesAsync(stoppingToken));
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "服务目录刷新失败，保留最后成功快照");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }
}
