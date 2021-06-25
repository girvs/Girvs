using System;

namespace Power.EventBus.User
{
    public class UpdateUserEvent : IntegrationEvent
    {
        /// <summary>
        /// 其它关联的主键(需要唯一)
        /// </summary>
        public Guid OtherId { get; set; }
        
        /// <summary>
        /// 用户名称
        /// </summary>
        public string UserName { get; set; }
        
        /// <summary>
        /// 联系号码
        /// </summary>
        public string ContactNumber { get; }

        public UpdateUserEvent(string contactNumber, Guid otherId, string userName)
        {
            ContactNumber = contactNumber;
            OtherId = otherId;
            UserName = userName;
        }
    }
}