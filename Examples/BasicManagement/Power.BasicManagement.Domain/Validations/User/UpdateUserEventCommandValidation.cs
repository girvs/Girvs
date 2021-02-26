using Power.BasicManagement.Domain.Commands.User;

namespace Power.BasicManagement.Domain.Validations.User
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