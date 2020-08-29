using Test.Domain.Commands.User;

namespace Test.Domain.Validations.User
{
    public class CreateUserCommandValidation : UserCommandValidation<CreateUserCommand>
    {
        public CreateUserCommandValidation()
        {
            ValidationName();
            ValidationContactNumber();
            ValidationUserAccount();
            ValidationUserPassword();
            ValidationContactNumber();
        }

        public override bool IsErrorMessageDelay { get; set; } = true;
    }
}