using Girvs.EventBus;
using System;
using System.Collections.Generic;

namespace ZhuoFan.Wb.Common.Events.Examinees
{
    /// <summary>
    /// 添加报考考生事件
    /// </summary>
    public class ExamineeWorkflowCreateEvent : IntegrationEvent
    {
        public ExamineeWorkflowCreateEvent()
        {

        }
        public ExamineeWorkflowCreateEvent(string name, string identityCard, string contactNumber, Guid organizationId, string organizationName, Guid unitId, string unitName, Guid positionId, string positionName, DateTime applicationTime, Guid? workflowId, string nodeId, string nodeCode, string nodeDisplayName, WorkflowRecordEvent workflowRecord, WorkflowRecordEvent nextWorkflowRecord, string toHandlers)
        {
            Name = name;
            IdentityCard = identityCard;
            ContactNumber = contactNumber;
            OrganizationId = organizationId;
            OrganizationName = organizationName;
            UnitId = unitId;
            UnitName = unitName;
            PositionId = positionId;
            PositionName = positionName;
            ApplicationTime = applicationTime;
            WorkflowId = workflowId;
            NodeId = nodeId;
            NodeCode = nodeCode;
            NodeDisplayName = nodeDisplayName;
            WorkflowRecord = workflowRecord;
            NextWorkflowRecord = nextWorkflowRecord;
            ToHandlers = toHandlers;
        }
        /// <summary>
        /// 考生姓名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 考生身份证
        /// </summary>
        public string IdentityCard { get; set; }

        /// <summary>
        /// 考生手机号
        /// </summary>
        public string ContactNumber { get; set; }

        /// <summary>
        /// 机构Id
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// 机构名称
        /// </summary>
        public string OrganizationName { get; set; }

        /// <summary>
        /// 单位Id
        /// </summary>
        public Guid UnitId { get; set; }

        /// <summary>
        /// 单位名称
        /// </summary>
        public string UnitName { get; set; }

        /// <summary>
        /// 职位Id
        /// </summary>
        public Guid PositionId { get; set; }

        /// <summary>
        /// 职位名称
        /// </summary>
        public string PositionName { get; set; }

        /// <summary>
        /// 报考流程Id
        /// </summary>
        public Guid? WorkflowId { get; set; }

        /// <summary>
        /// 当前节点Id
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// 当前节点Id
        /// </summary>
        public string NodeCode { get; set; }

        /// <summary>
        /// 当前节点名称
        /// </summary>
        public string NodeDisplayName { get; set; }

        /// <summary>
        /// 报考时间
        /// </summary>
        public DateTime ApplicationTime { get; set; }

        public string ToHandlers { get; set; }

        public WorkflowRecordEvent WorkflowRecord { get; set; }
        public WorkflowRecordEvent NextWorkflowRecord { get; set; }
    }
}
