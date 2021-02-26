using Power.BasicManagement.Domain.Commands.User;

namespace Power.BasicManagement.Domain.Validations.User
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