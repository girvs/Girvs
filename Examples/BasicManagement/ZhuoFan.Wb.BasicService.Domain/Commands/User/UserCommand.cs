using System;
using Girvs.Driven.Commands;
using ZhuoFan.Wb.BasicService.Domain.Enumerations;

namespace ZhuoFan.Wb.BasicService.Domain.Commands.User
{
    public abstract class UserCommand : Command
    {
        public Guid Id { get; set; }
        public string UserAccount { get; protected set; }

        public string UserPassword { get; protected set; }

        public string UserName { get; protected set; }

        public string ContactNumber { get; protected set; }
        
        /// <summary>
        /// 绑定其它相关服务的关键标识Id
        /// </summary>
        public Guid OtherId { get; set; }

        public DataState State { get; protected set; }

        public UserType UserType { get; protected set; }

        public string NewPassword { get; protected set; }
        public string OldPassword { get; protected set; }
        
        public Guid[] RoleIds { get; protected set; }
    }
}