using Girvs.EventBus;
using System;

namespace ZhuoFan.Wb.Common.Events.Examinees
{
    public class WorkflowRecordEvent
    {
        public WorkflowRecordEvent()
        {

        }
        public WorkflowRecordEvent(Guid? workflowId, string nodeId, string nodeCode, string nodeDisplayName, string nodeResult, string handler, string handlerName, string formData, string comment)
        {
            WorkflowId = workflowId;
            NodeId = nodeId;
            NodeCode = nodeCode;
            NodeDisplayName = nodeDisplayName;
            NodeResult = nodeResult;
            Handler = handler;
            HandlerName = handlerName;
            FormData = formData;
            Comment = comment;
        }
        /// <summary>
        /// 报考流程Id
        /// </summary>
        public Guid? WorkflowId { get;  set; }

        /// <summary>
        /// 节点Id
        /// </summary>
        public string NodeId { get;  set; }

        /// <summary>
        /// 节点编码
        /// </summary>
        public string NodeCode { get;  set; }

        /// <summary>
        /// 节点显示名称
        /// </summary>
        public string NodeDisplayName { get;  set; }

        /// <summary>
        /// 节点审核结果
        /// </summary>
        public string NodeResult { get;  set; }

        /// <summary>
        /// 审批操作人
        /// </summary>
        public string Handler { get;  set; }

        /// <summary>
        /// 审批操作人名称
        /// </summary>
        public string HandlerName { get;  set; }

        /// <summary>
        /// 表单参数
        /// </summary>
        public string FormData { get;  set; }

        /// <summary>
        /// 审核意见
        /// </summary>
        public string Comment { get;  set; }
    }
}
