using System;

namespace Test.Domain.Commands.User
{
    public class ChangeUserPassworkCommand : UserCommand
    {
        public ChangeUserPassworkCommand(Guid id, string oldPassword, string newPassword)
        {
            OldPassword = oldPassword;
            NewPassword = newPassword;
            Id = id;
        }

        public override bool IsValid()
        {
            throw new System.NotImplementedException();
        }
    }
}