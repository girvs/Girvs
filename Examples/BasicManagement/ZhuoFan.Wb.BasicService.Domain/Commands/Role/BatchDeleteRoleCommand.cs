using System;
using System.Collections.Generic;
using Girvs.Driven.Commands;

namespace ZhuoFan.Wb.BasicService.Domain.Commands.Role
{
    public class BatchDeleteRoleCommand:Command
    {
        public BatchDeleteRoleCommand(IList<Guid> ids)
        {
            Ids = ids;
        }

        public IList<Guid> Ids { get; private set; }    
        public override string CommandDesc { get; set; } = "批量删除角色";
    }
}