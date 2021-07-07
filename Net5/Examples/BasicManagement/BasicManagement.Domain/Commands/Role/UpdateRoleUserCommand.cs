using System;

namespace BasicManagement.Domain.Commands.Role
{
    public class UpdateRoleUserCommand : RoleCommand
    {
        public UpdateRoleUserCommand(Guid roleId, Guid[] userIds)
        {
            UserIds = userIds;
            Id = roleId;
        }


        public override bool IsValid()
        {
            throw new System.NotImplementedException();
        }

        public override string CommandDesc { get; set; } = "更新角色用户";
    }
}