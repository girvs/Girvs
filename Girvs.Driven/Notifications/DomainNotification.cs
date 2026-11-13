namespace Girvs.Driven.Notifications;

/// <summary>
/// 领域通知模型，用来获取当前总线中出现的通知信息
/// 继承自领域事件和 INotification（也就意味着可以拥有中介的发布/订阅模式）
/// </summary>
public record DomainNotification(string Key, string Value, int StatusCode = 568) : Event
{
    // 标识
    public Guid DomainNotificationId { get; private set; } = Guid.NewGuid();

    // 键（可以根据这个key，获取当前key下的全部通知信息）
    // 这个我们在事件源和事件回溯的时候会用到，伏笔
    public string Key { get; private set; } = Key;

    // 值（与key对应）
    public string Value { get; private set; } = Value;

    // 版本信息
    public int Version { get; private set; } = 1;

    public int StatusCode { get; private set; } = StatusCode;
}