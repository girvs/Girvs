using System;

namespace Power.BasicManagement.Domain.Commands.User
{
    public class UpdateUserEventCommand : UserCommand
    {
        public UpdateUserEventCommand(Guid otherId, string userName, string contactNumber)
        {
            OtherId = otherId;
            UserName = userName;
            ContactNumber = contactNumber;
        }
        
        
        public override string CommandDesc { get; set; } = "通过订阅事件更新用户";
    }
}