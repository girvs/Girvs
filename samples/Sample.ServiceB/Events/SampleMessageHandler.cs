using DotNetCore.CAP;
using DotNetCore.CAP.Messages;
using Girvs.EventBus;

namespace Sample.ServiceB.Events;

// 订阅者：CAP 扫描 [CapSubscribe] 方法。收到消息后记录到静态字段，供自检端点确认端到端投递。
public class SampleMessageHandler(IServiceProvider serviceProvider)
    : GirvsIntegrationEventHandler<SampleMessage>(serviceProvider)
{
    public static volatile string? LastReceived;

    [CapSubscribe(nameof(SampleMessage))]
    public override Task Handle(
        SampleMessage @event,
        // [FromCap] 让 CAP 注入消息头，而非试图从消息体反序列化 CapHeader（否则 NotSupportedException）
        [FromCap] CapHeader header,
        CancellationToken cancellationToken)
    {
        return HandleInScopeAsync(
            header,
            _ =>
            {
                LastReceived = @event.Text;
                return Task.CompletedTask;
            },
            cancellationToken);
    }
}
