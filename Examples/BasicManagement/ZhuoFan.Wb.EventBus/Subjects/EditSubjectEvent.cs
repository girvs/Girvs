using Girvs.EventBus;
using System;

namespace Subjects
{
    /// <summary>
    /// 科目修改名称事件
    /// </summary>
    public class EditSubjectEvent : IntegrationEvent
    {
        /// <summary>
        /// 科目主键
        /// </summary>
        public Guid SubjectId { get; set; }

        /// <summary>
        /// 科目名称
        /// </summary>
        public string SubjectName { get; set; }

        /// <summary>
        /// 科目费用
        /// </summary>
        public decimal SubjectCost { get; set; }
    }
}
