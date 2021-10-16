using Girvs.EventBus;
using System;

namespace ZhuoFan.Wb.Common.Events.Organizations
{
    /// <summary>
    /// 职位修改事件
    /// </summary>
    public class EditPositionEvent : IntegrationEvent
    {
        public EditPositionEvent()
        {

        }
        public EditPositionEvent(Guid positionId, string positionName)
        {
            PositionId = positionId;
            PositionName = positionName;
        }
        /// <summary>
        /// 职位Id
        /// </summary>
        public Guid PositionId { get;  set; }

        /// <summary>
        /// 职位名称
        /// </summary>
        public string PositionName { get;  set; }
    }
}
