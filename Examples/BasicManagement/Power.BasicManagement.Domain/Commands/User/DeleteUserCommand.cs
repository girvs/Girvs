using System;

namespace Power.BasicManagement.Domain.Commands.User
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
        
        public override string CommandDesc { get; set; } = "删除用户";
    }
}