using System;

namespace Test.Domain.Commands.User
{
    public class UpdateUserRoleCommand : UserCommand
    {
        public UpdateUserRoleCommand(Guid id, Guid[] roleIds)
        {
            Id = id;
            RoleIds = roleIds;
        }

        public override string CommandDesc { get; set; } = "更新用户角色";

        public override bool IsValid()
        {
            throw new System.NotImplementedException();
        }
    }
}