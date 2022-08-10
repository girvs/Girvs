namespace Girvs.Driven.Validations;

public abstract class GirvsCommandValidator<TCommand> : AbstractValidator<TCommand>
{
    public virtual bool IsErrorMessageDelay { get; set; } = false;
}