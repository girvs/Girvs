using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace Sample.ServiceA.Controllers;

[ApiController]
[Route("selfcheck")]
public class SelfCheckController(IDistributedCache cache) : ControllerBase
{
    /// <summary>
    /// 缓存自检：写入一个键再读回，验证 Aspire 注入的 Redis 连接串在运行时真实可用
    /// </summary>
    [HttpGet("cache")]
    public async Task<IActionResult> Cache()
    {
        var value = $"ok-{Guid.NewGuid():N}";
        await cache.SetStringAsync("selfcheck", value);
        var readBack = await cache.GetStringAsync("selfcheck");
        return Ok(new { match = readBack == value, value = readBack });
    }
}
