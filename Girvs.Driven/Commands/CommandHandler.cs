namespace Girvs.Driven.Commands;

public abstract class CommandHandler(IUnitOfWork uow, IMediatorHandler bus)
{
    protected void NotifyValidationErrors(Command message)
    {
        foreach (var error in message.ValidationResult.Errors)
        {
            bus.RaiseEvent(new DomainNotification("", error.ErrorMessage));
        }
    }

    public Task<bool> Commit() => uow.Commit();
}
