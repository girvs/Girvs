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

        public override bool IsValid()
        {
            throw new System.NotImplementedException();
        }
    }
}