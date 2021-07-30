using ZhuoFan.Wb.BasicService.Domain.Commands.User;

namespace ZhuoFan.Wb.BasicService.Domain.Validations.User
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