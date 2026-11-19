namespace Girvs.EventBus;

/// <summary>
/// Girvs 集成事件处理器。CAP 过滤器会在调用 <see cref="Handle"/> 前自动恢复身份和服务作用域。
/// <para>
/// Handle 全程执行于本消息的专属作用域内，Handle 内经 EngineContext.Resolve 取得的服务
/// 与构造注入落在同一作用域（同一 DbContext 实例）。
/// </para>
/// <para>
/// 警示：Handle 中不得启动跨消息生命周期的 fire-and-forget 任务（如未 await 的 Task.Run）
/// 并在其中经 EngineContext.Resolve 取 Scoped 服务——消息处理结束作用域即释放，
/// 此类用法会得到明确失败。
/// </para>
/// </summary>
public abstract class GirvsIntegrationEventHandler<TIntegrationEvent>
    : IIntegrationEventHandler<TIntegrationEvent>
    where TIntegrationEvent : IntegrationEvent
{
    public abstract Task Handle(
        TIntegrationEvent @event,
        CapHeader header,
        CancellationToken cancellationToken
    );
}
