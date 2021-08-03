using Girvs.EventBus;

namespace Users
{
    public class CreateUserEvent : IntegrationEvent
    {
        public string UserAccount { get; protected set; }

        public string UserPassword { get; protected set; }

        public string UserName { get; protected set; }

        public string ContactNumber { get; protected set; }
        
        public UserType UserType { get; protected set; }
    }


    public enum UserType
    {
        /// <summary>
        /// 超级管理员
        /// </summary>
        SuperAdmin,
        /// <summary>
        /// 单位管理员
        /// </summary>
        UnitAdmin,
        /// <summary>
        /// 单位用户
        /// </summary>
        UnitPersion
    }
}
