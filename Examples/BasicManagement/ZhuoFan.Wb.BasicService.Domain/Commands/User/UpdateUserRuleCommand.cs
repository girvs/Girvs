using System;
using System.Collections.Generic;
using Girvs.Driven.Commands;
using ZhuoFan.Wb.BasicService.Domain.Models;

namespace ZhuoFan.Wb.BasicService.Domain.Commands.User
{
    public class UpdateUserRuleCommand : Command
    {
        public UpdateUserRuleCommand(Guid userId, List<UserRule> userRules)
        {
            UserId = userId;
            UserRules = userRules;
        }

        public override string CommandDesc { get; set; } = "更新用户的数据权限";

        public Guid UserId { get; private set; }

        public List<UserRule> UserRules { get; private set; }
    }
}