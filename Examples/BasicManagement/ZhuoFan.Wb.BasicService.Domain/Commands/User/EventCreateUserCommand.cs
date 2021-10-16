using System;
using Girvs.AuthorizePermission.Enumerations;
using ZhuoFan.Wb.BasicService.Domain.Enumerations;

namespace ZhuoFan.Wb.BasicService.Domain.Commands.User
{
    public class EventCreateUserCommand : UserCommand
    {
        public override string CommandDesc { get; set; } = "通过事件创建用户";

        public EventCreateUserCommand(string userAccount, string userPassword, string userName, string contactNumber,
            DataState state, UserType userType, Guid tenantId) : this(userAccount, userPassword, userName,
            contactNumber, state,
            userType, Guid.Empty, tenantId)
        {
        }

        public EventCreateUserCommand(string userAccount, string userPassword, string userName, string contactNumber,
            DataState state, UserType userType, Guid otherId, Guid tenantId)

        {
            OtherId = otherId;
            UserAccount = userAccount;
            UserPassword = userPassword;
            UserName = userName;
            ContactNumber = contactNumber;
            State = state;
            UserType = userType;
            TenantId = tenantId;
        }
    }
}