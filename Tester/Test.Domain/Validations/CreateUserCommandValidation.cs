using FluentValidation;
using Test.Domain.Commands.User;

namespace Test.Domain.Validations
{
    public class CreateUserCommandValidation : AbstractValidator<CreateUserCommand>
    {
        public CreateUserCommandValidation()
        {
            ValidateBirthDate();
        }


        protected void ValidateBirthDate()
        {
            RuleFor(c => c.UserName)
                .NotEmpty().WithMessage("用户名称不能为空")
                .Length(4, 10).WithMessage("用户名称在4~10个字符之间");
        }
    }
}