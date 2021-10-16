using System;

namespace ZhuoFan.Wb.BasicService.Domain.Commands.User
{
    public class UserEditPasswordCommand : UserCommand
    {
        public UserEditPasswordCommand(Guid id, string oldPassword,string newPassword)
        {
            OldPassword = oldPassword;
            NewPassword = newPassword;
            Id = id;
        }

        public override string CommandDesc { get; set; } = "用户自身修改密码";
    }
}