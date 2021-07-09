using ZhuoFan.Wb.BasicService.Domain.Commands.User;

namespace ZhuoFan.Wb.BasicService.Domain.Validations.User
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