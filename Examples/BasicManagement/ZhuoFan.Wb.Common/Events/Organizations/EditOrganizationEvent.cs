using System;
using Girvs.EventBus;

namespace ZhuoFan.Wb.Common.Events.Organizations
{
    /// <summary>
    /// 机构修改事件
    /// </summary>
    public class EditOrganizationEvent : IntegrationEvent
    {
        public EditOrganizationEvent()
        {

        }
        public EditOrganizationEvent(Guid organizationId, string organizationName)
        {
            OrganizationId = organizationId;
            OrganizationName = organizationName;
        }
        /// <summary>
        /// 机构Id
        /// </summary>
        public Guid OrganizationId { get;  set; }

        /// <summary>
        /// 机构名称
        /// </summary>
        public string OrganizationName { get;  set; }
    }
}