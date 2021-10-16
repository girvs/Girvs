using System;
using Girvs.EventBus;

namespace ZhuoFan.Wb.Common.Events.Subjects
{
    /// <summary>
    /// 科目修改名称事件
    /// </summary>
    public class EditSubjectEvent : IntegrationEvent
    {
        public EditSubjectEvent()
        {

        }
        public EditSubjectEvent(Guid subjectId, string subjectName, decimal subjectCost)
        {
            SubjectId = subjectId;
            SubjectName = subjectName;
            SubjectCost = subjectCost;
        }
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
