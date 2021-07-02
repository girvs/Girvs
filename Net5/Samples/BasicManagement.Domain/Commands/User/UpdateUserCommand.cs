using System;
using BasicManagement.Domain.Enumerations;

namespace BasicManagement.Domain.Commands.User
{
    public class UpdateUserCommand : UserCommand
    {
        public UpdateUserCommand(Guid id, string userPassword, string userName, string contactNumber,
            DataState state, UserType userType)

        {
            Id = id;
            UserPassword = userPassword;
            UserName = userName;
            ContactNumber = contactNumber;
            State = state;
            UserType = userType;
        }

        public override bool IsValid()
        {
            throw new System.NotImplementedException();
        }
        public override string CommandDesc { get; set; } = "更新用户";
    }
}