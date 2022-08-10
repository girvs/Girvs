namespace Girvs.Driven.Behaviors;

public interface ICommandOperateHandler : IManager
{
    Task Handle(Command command,CancellationToken cancellationToken = default(CancellationToken));
}