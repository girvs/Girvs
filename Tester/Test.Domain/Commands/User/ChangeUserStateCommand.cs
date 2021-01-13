using System;
using Test.Domain.Enumerations;

namespace Test.Domain.Commands.User
{
    public class ChangeUserStateCommand : UserCommand
    {
        public ChangeUserStateCommand(Guid id, DataState state)

        {
            Id = id;
            State = state;
        }

        public override string CommandDesc { get; set; } = "更新用户状态";

        public override bool IsValid()
        {
            return true;
        }
    }
}