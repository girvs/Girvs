using BasicManagement.Domain.Commands.User;

namespace BasicManagement.Domain.Validations.User
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