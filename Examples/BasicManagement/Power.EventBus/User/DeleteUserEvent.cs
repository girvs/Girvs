using System;

namespace Power.EventBus.User
{
    public class DeleteUserEvent : IntegrationEvent
    {
        public DeleteUserEvent(Guid otherId)
        {
            OtherId = otherId;
        }

        /// <summary>
        /// 关联关键Id
        /// </summary>
        public Guid OtherId { get; set; }
    }
}