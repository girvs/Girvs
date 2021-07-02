﻿using System;
using BasicManagement.Domain.Enumerations;

namespace BasicManagement.Domain.Commands.User
{
    public class ChangeUserStateCommand : UserCommand
    {
        public ChangeUserStateCommand(Guid id, DataState state)

        {
            Id = id;
            State = state;
        }
        public override bool IsValid()
        {
            return true;
        }
        
        
        public override string CommandDesc { get; set; } = "更新用户状态";
    }
}