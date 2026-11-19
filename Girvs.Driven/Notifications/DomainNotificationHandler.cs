namespace Girvs.Driven.Notifications;

/// <summary>
/// 领域通知处理程序，把所有的通知信息放到事件总线中
/// 继承 INotificationHandler<>
/// </summary>
public class DomainNotificationHandler : INotificationHandler<DomainNotification>, IDisposable
{
    // 通知信息列表
    private List<DomainNotification> _notifications = [];

    // 保护通知列表的并发访问
    private readonly object _lock = new();

    // 每次访问该处理程序的时候，实例化一个空集合

    // 处理方法，把全部的通知信息，添加到内存里
    public Task Handle(DomainNotification message, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            _notifications.Add(message);
        }

        return Task.CompletedTask;
    }

    // 获取当前生命周期内的全部通知信息
    public virtual IEnumerable<DomainNotification> GetNotifications()
    {
        lock (_lock)
        {
            // 返回快照，防止锁外的延迟枚举与并发修改冲突
            return _notifications.ToList();
        }
    }

    public virtual Dictionary<string, IList<string>> GetNotificationMessage()
    {
        var error = new Dictionary<string, IList<string>>();

        lock (_lock)
        {
            if (!_notifications.Any()) return error;

            _notifications.ForEach(x =>
            {
                if (!error.ContainsKey(x.Key))
                    error.Add(x.Key, new List<string>());

                error[x.Key].Add(x.Value);
            });
        }

        return error;
    }

    // 判断在当前总线对象周期中，是否存在通知信息
    public virtual bool HasNotifications()
    {
        lock (_lock)
        {
            return _notifications.Any();
        }
    }

    // 手动回收（清空通知）
    public void Dispose()
    {
        lock (_lock)
        {
            _notifications = new List<DomainNotification>();
        }
    }
}