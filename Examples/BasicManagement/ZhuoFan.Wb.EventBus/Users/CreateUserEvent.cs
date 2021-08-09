using System.ComponentModel;
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
        [Description("超级管理员")]
        SuperAdmin,
        
        /// <summary>
        /// 考试管理员
        /// </summary>
        [Description("考试管理员")]
        ExamAdmin,
        
        /// <summary>
        /// 机构管理员
        /// </summary>
        [Description("机构用户")]
        OrganizationUser,

        /// <summary>
        /// 单位管理员
        /// </summary>
        [Description("单位用户")]
        UnitUser,
        
        /// <summary>
        /// 授权用户
        /// </summary>
        [Description("单位管理员")]
        AuthorizeUser
    }
}
