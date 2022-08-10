namespace Girvs.Driven.Commands;

public abstract class CommandHandler
{
    private readonly IUnitOfWork _uow;
    private readonly IMediatorHandler _bus;

    public CommandHandler(IUnitOfWork uow, IMediatorHandler bus)
    {
        _uow = uow;
        _bus = bus;
    }


    protected void NotifyValidationErrors(Command message)
    {
        foreach (var error in message.ValidationResult.Errors)
        {
            _bus.RaiseEvent(new DomainNotification("", error.ErrorMessage));
        }
    }

    public Task<bool> Commit()
    {
        return _uow.Commit();
    }
}