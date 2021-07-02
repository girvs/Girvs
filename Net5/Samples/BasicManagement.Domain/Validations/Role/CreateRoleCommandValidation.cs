using BasicManagement.Domain.Commands.Role;

namespace BasicManagement.Domain.Validations.Role
{
    public class CreateRoleCommandValidation : RoleCommandValidation<CreateRoleCommand>
    {
        public CreateRoleCommandValidation()
        {
            ValidationName();
        }
    }
}