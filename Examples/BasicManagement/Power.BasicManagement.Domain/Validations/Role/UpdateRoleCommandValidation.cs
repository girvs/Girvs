using Power.BasicManagement.Domain.Commands.Role;

namespace Power.BasicManagement.Domain.Validations.Role
{
    public class UpdateRoleCommandValidation : RoleCommandValidation<UpdateRoleCommand>
    {
        public UpdateRoleCommandValidation()
        {
            ValidationName();
        }
    }
}