using FluentValidation;

namespace Girvs.Domain.Driven.Validations
{
    public abstract class GirvsCommandValidator<TCommand> : AbstractValidator<TCommand>
    {
        public virtual bool IsErrorMessageDelay { get; set; } = false;
    }
}