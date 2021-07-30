using ZhuoFan.Wb.BasicService.Domain.Commands.Role;

namespace ZhuoFan.Wb.BasicService.Domain.Validations.Role
{
    public class UpdateRoleCommandValidation : RoleCommandValidation<UpdateRoleCommand>
    {
        public UpdateRoleCommandValidation()
        {
            ValidationName();
        }
    }
}