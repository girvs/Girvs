using ZhuoFan.Wb.BasicService.Domain.Commands.User;

namespace ZhuoFan.Wb.BasicService.Domain.Validations.User
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