using System;

namespace ZhuoFan.Wb.BasicService.Domain.Commands.User
{
    public class ChangeUserPassworkCommand : UserCommand
    {
        public ChangeUserPassworkCommand(Guid id, string newPassword)
        {
            NewPassword = newPassword;
            Id = id;
        }

        public override string CommandDesc { get; set; } = "修改用户密码";
    }
}