using System;

namespace Test.Domain.Commands.Role
{
    public class DeleteRoleCommand : RoleCommand
    {
        public DeleteRoleCommand(Guid id)
        {
            Id = id;
        }

        public override bool IsValid()
        {
            return true;
        }
    }
}