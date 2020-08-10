using FluentValidation;
using Girvs.Domain.Driven.Validations;
using Test.Domain.Commands.User;

namespace Test.Domain.Validations
{
    public class CreateUserCommandValidation : GirvsCommandValidator<CreateUserCommand>
    {
        public CreateUserCommandValidation()
        {
            ValidateBirthDate();
        }

        public override bool IsErrorMessageDelay { get; set; } = false;

        protected void ValidateBirthDate()
        {
            RuleFor(c => c.UserName)
                .NotEmpty().WithMessage("用户名称不能为空")
                .Length(4, 10).WithMessage("用户名称在4~10个字符之间");
        }
    }
}