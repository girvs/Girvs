using System;

namespace Power.BasicManagement.Domain.Commands.Role
{
    public class DeleteRoleCommand : RoleCommand
    {
        public override string CommandDesc { get; set; } = "删除角色";
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