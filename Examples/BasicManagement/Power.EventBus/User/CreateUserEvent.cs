using System;

namespace Power.EventBus.User
{
    public class CreateUserEvent : IntegrationEvent
    {
        /// <summary>
        /// 用户登陆名称
        /// </summary>
        public string UserAccount { get; set; }
        
        /// <summary>
        /// 其它关联的主键(需要唯一)
        /// </summary>
        public Guid OtherId { get; set; }
        
        /// <summary>
        /// 用户名称
        /// </summary>
        public string UserName { get; set; }
        
        /// <summary>
        /// 用户登陆密码
        /// </summary>
        public string UserPassword { get; set; }
        
        /// <summary>
        /// 联系号码
        /// </summary>
        public string ContactNumber { get; }

        public CreateUserEvent(string userAccount, string userPassword, string contactNumber, Guid otherId, string userName)
        {
            UserAccount = userAccount;
            UserPassword = userPassword;
            ContactNumber = contactNumber;
            OtherId = otherId;
            UserName = userName;
        }
    }
}