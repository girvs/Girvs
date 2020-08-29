using Test.Domain.Commands.User;

namespace Test.Domain.Validations.User
{
    public class ChangePasswordUserCommandValidation : UserCommandValidation<ChangeUserPassworkCommand>
    {
        public ChangePasswordUserCommandValidation()
        {
            ValidationNewPassword();
            ValidationOldPassword();
        }
    }
}