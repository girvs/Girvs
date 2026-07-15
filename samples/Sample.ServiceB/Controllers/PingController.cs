using Microsoft.AspNetCore.Mvc;

namespace Sample.ServiceB.Controllers;

[ApiController]
public class PingController : ControllerBase
{
    // 被 ServiceA 通过服务发现（http://service-b/ping）调用
    [HttpGet("/ping")]
    public IActionResult Ping() => Ok("pong");
}
