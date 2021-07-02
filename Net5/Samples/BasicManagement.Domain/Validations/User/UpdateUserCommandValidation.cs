using BasicManagement.Domain.Commands.User;

namespace BasicManagement.Domain.Validations.User
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