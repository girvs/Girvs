namespace Girvs.Driven.Validations;

public abstract class GirvsCommandValidator<TCommand> : AbstractValidator<TCommand> where TCommand : IBaseRequest
{
    public GirvsCommandValidator()
    {
        
    }
    
    public virtual bool IsErrorMessageDelay { get; set; } = false;
}