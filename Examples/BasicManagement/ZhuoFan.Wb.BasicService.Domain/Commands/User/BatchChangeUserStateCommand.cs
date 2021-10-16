using System;
using System.Collections.Generic;
using Girvs.Driven.Commands;
using ZhuoFan.Wb.BasicService.Domain.Enumerations;

namespace ZhuoFan.Wb.BasicService.Domain.Commands.User
{
    public class BatchChangeUserStateCommand : Command
    {
        public BatchChangeUserStateCommand(IList<Guid> ids, DataState dataState)
        {
            Ids = ids;
            DataState = dataState;
        }

        public override string CommandDesc { get; set; } = "批量修改用户状态";

        public IList<Guid> Ids { get; private set; }
        public DataState DataState { get; private set; }
    }
}