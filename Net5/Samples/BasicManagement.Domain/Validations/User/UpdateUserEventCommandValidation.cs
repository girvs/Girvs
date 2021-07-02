using BasicManagement.Domain.Commands.User;

namespace BasicManagement.Domain.Validations.User
{
    public class UpdateUserEventCommandValidation : UserCommandValidation<UpdateUserEventCommand>
    {
        public UpdateUserEventCommandValidation()
        {
            ValidateOtherId();
            ValidationContactNumber();
            ValidationName();
        }
    }
}