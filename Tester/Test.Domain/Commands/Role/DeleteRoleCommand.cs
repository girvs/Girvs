using System;

namespace Test.Domain.Commands.Role
{
    public class DeleteRoleCommand : RoleCommand
    {
        public DeleteRoleCommand(Guid id)
        {
            Id = id;
        }

        public override string CommandDesc { get; set; } = "删除角色";

        public override bool IsValid()
        {
            return true;
        }
    }
}