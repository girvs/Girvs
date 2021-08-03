using System;
using Girvs.EventBus;

namespace AuditLogs
{
    public class CreateAuditLogEvent : IntegrationEvent
    {
        /// <summary>
        /// 服务模块名称
        /// </summary>
        public string ServiceModuleName { get; set; }

        /// <summary>
        /// 消息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 消息来源
        /// </summary>
        public SourceType SourceType { get; set; }

        /// <summary>
        /// Ip地址来源
        /// </summary>
        public string AddressIp { get; set; }

        /// <summary>
        /// 租户ID
        /// </summary>
        public Guid TenantId { get; set; }
        
        /// <summary>
        /// 租户名称
        /// </summary>
        public string TenantName { get; set; }

        /// <summary>
        /// 具体消息内容
        /// </summary>
        public string MessageContent { get; set; }

        public Guid CreatorId { get; set; }
        public string CreatorName { get; set; }
    }

    public enum SourceType
    {
        /// <summary>
        /// 服务端
        /// </summary>
        Server,
        
        /// <summary>
        /// 考生端
        /// </summary>
        Register
    }
}