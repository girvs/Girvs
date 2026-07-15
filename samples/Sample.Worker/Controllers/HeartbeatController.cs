using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace Sample.Worker.Controllers;

[ApiController]
[Route("selfcheck")]
public class HeartbeatController : ControllerBase
{
    /// <summary>
    /// 读回后台 Worker 最近写入的心跳，验证后台服务的缓存连接串注入 + 后台任务在跑
    /// </summary>
    [HttpGet("heartbeat")]
    public async Task<IActionResult> Heartbeat([FromServices] IDistributedCache cache)
    {
        var value = await cache.GetStringAsync("worker:heartbeat");
        return Ok(new { lastHeartbeat = value });
    }
}
