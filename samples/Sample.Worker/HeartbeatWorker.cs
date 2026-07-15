using Microsoft.Extensions.Caching.Distributed;

namespace Sample.Worker;

/// <summary>
/// 后台工作负载：每 3 秒往 Girvs 缓存（Aspire 注入的 Redis）写一个心跳时间戳，
/// 证明 AddGirvsProject 编排的后台服务同样能用上组件的连接串自动注入。
/// </summary>
public class HeartbeatWorker(IDistributedCache cache, ILogger<HeartbeatWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var ts = DateTimeOffset.UtcNow.ToString("O");
            await cache.SetStringAsync("worker:heartbeat", ts, stoppingToken);
            logger.LogInformation("Worker 心跳已写入缓存：{Timestamp}", ts);
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
    }
}
