using System;
using System.Collections.Generic;
using Girvs.Driven.Commands;

namespace ZhuoFan.Wb.BasicService.Domain.Commands.User
{
    public class AddUserRoleCommand : Command
    {
        public AddUserRoleCommand(Guid userId, IList<Guid> roleIds)
        {
            UserId = userId;
            RoleIds = roleIds;
        }

        public override string CommandDesc { get; set; } = "添加用户角色";

        public Guid UserId { get; private set; }
        public IList<Guid> RoleIds { get; private set; }
    }
}