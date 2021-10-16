using Girvs.EventBus;
using System;
using System.Collections.Generic;

namespace ZhuoFan.Wb.Common.Events.Examinees
{
    public class ExamineeWrokflowUpdateEvent : IntegrationEvent
    {
        public ExamineeWrokflowUpdateEvent()
        {

        }
        public ExamineeWrokflowUpdateEvent(Guid? workflowId, string workflowResult, int? result, int? paymentResult, int? auditResult, int? pdAuditResult, string nodeId, string nodeCode, string nodeDisplayName, string paymentNo, List<string> assigns, WorkflowRecordEvent workflowRecord, WorkflowRecordEvent nextWorkflowRecord, string toHandlers)
        {
            WorkflowId = workflowId;
            WorkflowResult = workflowResult;
            Result = result;
            PaymentResult = paymentResult;
            AuditResult = auditResult;
            PdAuditResult = pdAuditResult;
            NodeId = nodeId;
            NodeCode = nodeCode;
            NodeDisplayName = nodeDisplayName;
            PaymentNo = paymentNo;
            Assigns = assigns;
            WorkflowRecord = workflowRecord;
            NextWorkflowRecord = nextWorkflowRecord;
            ToHandlers = toHandlers;
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
        /// 0:报名成功 ,1:报名中, 2:取消, 3:拒绝, 4:作废, 5:提交报名
        /// </summary>
        public int? Result { get; set; }

        /// <summary>
        /// 缴费状态
        /// 0:待缴费, 1:缴费中, 2:已缴费, 3:缴费失败, 4: 缴费中(减免审核中), 5:已缴费(申请减免), 6:缴费失败(减免审核不通过)
        /// </summary>
        public int? PaymentResult { get; set; }

        /// <summary>
        /// 审核状态
        /// 0:待审核, 1:审核通过, 2:审核不通过
        /// </summary>
        public int? AuditResult { get; set; }

        /// <summary>
        /// 审核状态
        /// 0:待审核, 1:审核通过, 2:审核不通过
        /// </summary>
        public int? PdAuditResult { get; set; }

        /// <summary>
        /// 当前节点Id
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// 当前节点编码
        /// </summary>
        public string NodeCode { get; set; }

        /// <summary>
        /// 当前节点名称
        /// </summary>
        public string NodeDisplayName { get; set; }

        /// <summary>
        /// 支付订单号
        /// </summary>
        public string PaymentNo { get; set; }

        /// <summary>
        /// 报名点
        /// </summary>
        public List<string> Assigns { get; set; }

        public string ToHandlers { get; set; }

        public WorkflowRecordEvent WorkflowRecord { get; set; }
        public WorkflowRecordEvent NextWorkflowRecord { get; set; }
    }
}
