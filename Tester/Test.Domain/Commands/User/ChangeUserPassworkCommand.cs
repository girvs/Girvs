﻿using System;

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

        public override string CommandDesc { get; set; } = "更新用户密码";

        public override bool IsValid()
        {
            throw new System.NotImplementedException();
        }
    }
}