using BasicManagement.Domain.Commands.Role;

namespace BasicManagement.Domain.Validations.Role
{
    public class UpdateRoleCommandValidation : RoleCommandValidation<UpdateRoleCommand>
    {
        public UpdateRoleCommandValidation()
        {
            ValidationName();
        }
    }
}