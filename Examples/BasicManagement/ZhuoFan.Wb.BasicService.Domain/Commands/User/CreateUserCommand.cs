using System;
using ZhuoFan.Wb.BasicService.Domain.Enumerations;

namespace ZhuoFan.Wb.BasicService.Domain.Commands.User
{
    public class CreateUserCommand : UserCommand
    {
        public CreateUserCommand(string userAccount, string userPassword, string userName, string contactNumber,
            DataState state, UserType userType) : this(userAccount, userPassword, userName, contactNumber, state,
            userType, Guid.Empty)
        {
        }

        public CreateUserCommand(string userAccount, string userPassword, string userName, string contactNumber,
            DataState state, UserType userType, Guid otherId)

        {
            OtherId = otherId;
            UserAccount = userAccount;
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
        
        public override string CommandDesc { get; set; } = "创建用户";
    }
}