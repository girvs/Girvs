using System;
using System.Collections.Generic;
using Girvs.Driven.Commands;

namespace ZhuoFan.Wb.BasicService.Domain.Commands.User
{
    public class BatchChangeUserPasswordCommand : Command
    {
        public BatchChangeUserPasswordCommand(IList<Guid> ids, string newPassword)
        {
            Ids = ids;
            NewPassword = newPassword;
        }

        public override string CommandDesc { get; set; } = "批量修改用户状态";

        public IList<Guid> Ids { get; private set; }
        public string NewPassword { get; private set; }
    }
}