using ZhuoFan.Wb.BasicService.Domain.Commands.Role;

namespace ZhuoFan.Wb.BasicService.Domain.Validations.Role
{
    public class CreateRoleCommandValidation : RoleCommandValidation<CreateRoleCommand>
    {
        public CreateRoleCommandValidation()
        {
            ValidationName();
        }
    }
}