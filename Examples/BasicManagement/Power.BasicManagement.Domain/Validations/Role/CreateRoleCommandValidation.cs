using Power.BasicManagement.Domain.Commands.Role;

namespace Power.BasicManagement.Domain.Validations.Role
{
    public class CreateRoleCommandValidation : RoleCommandValidation<CreateRoleCommand>
    {
        public CreateRoleCommandValidation()
        {
            ValidationName();
        }
    }
}