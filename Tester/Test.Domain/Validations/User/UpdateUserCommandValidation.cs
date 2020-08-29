using Test.Domain.Commands.User;

namespace Test.Domain.Validations.User
{
    public class UpdateUserCommandValidation: UserCommandValidation<UpdateUserCommand>
    {
        public UpdateUserCommandValidation()
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