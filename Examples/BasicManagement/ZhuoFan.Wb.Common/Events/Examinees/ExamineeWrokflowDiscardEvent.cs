namespace ZhuoFan.Wb.Common.Events.Examinees
{
    public record ExamineeWrokflowDiscardEvent : IntegrationEvent
    {
        public ExamineeWrokflowDiscardEvent()
        {

        }
        public ExamineeWrokflowDiscardEvent(Guid? workflowId, string workflowResult, int? result, WorkflowRecordEvent workflowRecord)
        {
            WorkflowId = workflowId;
            WorkflowResult = workflowResult;
            Result = result;
            WorkflowRecord = workflowRecord;
        }
        /// <summary>
        /// 报考流程Id
        /// </summary>
        public Guid? WorkflowId { get; set; }

        /// <summary>
        /// 报考流程结果
        /// </summary>
        public string WorkflowResult { get; set; }

        /// <summary>
        /// 报名结果
        /// 0:报名成功 ,1:报名中, 2:待审核, 3:审核通过, 4:审核不通过, 5:取消, 6:拒绝, 7:作废
        /// </summary>
        public int? Result { get; protected set; }
        public WorkflowRecordEvent WorkflowRecord { get; set; }
    }
}
