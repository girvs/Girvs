using Girvs.EventBus;
using Microsoft.AspNetCore.Mvc;
using Sample.ServiceB.Events;

namespace Sample.ServiceB.Controllers;

[ApiController]
[Route("selfcheck")]
public class SelfCheckController : ControllerBase
{
    /// <summary>
    /// 事件总线自检：发布一条 CAP 消息，验证 Aspire 注入的 RabbitMQ + MySQL(CAP 存储) 运行时可用
    /// </summary>
    [HttpGet("eventbus")]
    public async Task<IActionResult> Publish([FromServices] IEventBus bus)
    {
        var text = $"msg-{Guid.NewGuid():N}";
        await bus.PublishAsync(new SampleMessage(text));
        return Ok(new { published = true, text });
    }

    /// <summary>
    /// 返回订阅者最近收到的消息，用于确认 发布→RabbitMQ→消费 的端到端投递
    /// </summary>
    [HttpGet("eventbus/received")]
    public IActionResult Received()
        => Ok(new { lastReceived = SampleMessageHandler.LastReceived });

    /// <summary>
    /// 共享配置自检：回显由 AppHost 一处声明、注入到所有服务的通用配置（日志等级 + JwtSecret 是否有值）
    /// </summary>
    [HttpGet("config")]
    public IActionResult Config([FromServices] IConfiguration config) =>
        Ok(new
        {
            logLevel = config["Logging:LogLevel:Default"],
            jwtSecretPresent = !string.IsNullOrEmpty(config["Jwt:Secret"])
        });
}
