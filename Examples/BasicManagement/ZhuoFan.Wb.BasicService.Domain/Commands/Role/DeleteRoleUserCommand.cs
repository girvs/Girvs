using System;
using System.Collections.Generic;
using Girvs.Driven.Commands;

namespace ZhuoFan.Wb.BasicService.Domain.Commands.Role
{
    public class DeleteRoleUserCommand:Command
    {
        public DeleteRoleUserCommand(Guid roleId, IList<Guid> userIds)
        {
            RoleId = roleId;
            UserIds = userIds;
        }

        public override string CommandDesc { get; set; } = "删除角色用户";
        public Guid RoleId { get; private set; }
        public IList<Guid> UserIds { get; private set; }
    }
}