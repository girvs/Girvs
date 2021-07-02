using System;

namespace BasicManagement.Domain.Commands.Role
{
    public class UpdateRoleCommand : RoleCommand
    {
        public UpdateRoleCommand(Guid id, string name,string desc, Guid[] userIds)
        {
            Id = id;
            Name = name;
            Desc = desc;
            UserIds = userIds;
        }

        public override bool IsValid()
        {
            return true;
        }

        public override string CommandDesc { get; set; } = "更新角色";
    }
}