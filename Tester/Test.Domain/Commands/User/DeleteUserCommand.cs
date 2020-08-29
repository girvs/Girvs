using System;

namespace Test.Domain.Commands.User
{
    public class DeleteUserCommand : UserCommand
    {
        public DeleteUserCommand(Guid id)
        {
            Id = id;
        }

        public override bool IsValid()
        {
            return true;
        }
    }
}