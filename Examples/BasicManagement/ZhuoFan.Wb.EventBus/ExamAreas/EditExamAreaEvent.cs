using System;
using Girvs.EventBus;

namespace ExamAreas
{
    /// <summary>
    /// 考区修改事件
    /// </summary>
    public class EditExamAreaEvent : IntegrationEvent
    {
        /// <summary>
        /// 考区Id
        /// </summary>
        public Guid ExamAreaId { get; set; }

        /// <summary>
        /// 考区代码
        /// </summary>
        public string ExamAreaCode { get; set; }

        /// <summary>
        /// 考区名称
        /// </summary>
        public string ExamAreaName { get; set; }
    }
}
