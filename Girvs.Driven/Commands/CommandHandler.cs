namespace Girvs.Driven.Commands;

public abstract class CommandHandler(IUnitOfWork uow, IMediatorHandler bus)
{
    /// <summary>
    /// 无工作单元的构造重载，适用于不需要提交变更的处理器（如缓存命令处理器）
    /// </summary>
    protected CommandHandler(IMediatorHandler bus)
        : this(null, bus) { }

    protected void NotifyValidationErrors(Command message)
    {
        foreach (var error in message.ValidationResult.Errors)
        {
            bus.RaiseEvent(new DomainNotification("", error.ErrorMessage));
        }
    }

    public Task<bool> Commit()
    {
        return uow.Commit();
    }
}
