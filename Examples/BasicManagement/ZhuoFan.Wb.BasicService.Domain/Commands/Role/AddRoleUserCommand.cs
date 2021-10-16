using System;
using System.Collections.Generic;
using Girvs.Driven.Commands;

namespace ZhuoFan.Wb.BasicService.Domain.Commands.Role
{
    public class AddRoleUserCommand : Command
    {
        public AddRoleUserCommand(Guid roleId, IList<Guid> userIds)
        {
            RoleId = roleId;
            UserIds = userIds;
        }

        public override string CommandDesc { get; set; } = "添加角色用户";

        public Guid RoleId { get; private set; }

        public IList<Guid> UserIds { get; private set; }
    }
}