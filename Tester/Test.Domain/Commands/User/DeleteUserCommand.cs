using System;

namespace Test.Domain.Commands.User
{
    public class DeleteUserCommand : UserCommand
    {
        public DeleteUserCommand(Guid id)
        {
            Id = id;
        }

        public override string CommandDesc { get; set; } = "删除用户";

        public override bool IsValid()
        {
            return true;
        }
    }
}