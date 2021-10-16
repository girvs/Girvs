using System;
using System.Collections.Generic;
using Girvs.Driven.Commands;

namespace ZhuoFan.Wb.BasicService.Domain.Commands.User
{
    public class BatchDeleteUserCommand:Command
    {
        public BatchDeleteUserCommand(IList<Guid> ids)
        {
            Ids = ids;
        }

        public override string CommandDesc { get; set; } = "批量删除用户";

        public IList<Guid> Ids { get; private set; }
    }
}