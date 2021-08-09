namespace Girvs.Driven.Events
{
    public class MessageSource
    {
        /// <summary>
        /// 消息来源的操作者
        /// </summary>
        public string SourceName { get; set; }
        
        /// <summary>
        /// 消息来源的IP地址
        /// </summary>
        public string IpAddress { get; set; }
        
        /// <summary>
        /// 消息来源的操作者ID
        /// </summary>
        public string SourceNameId { get; set; }

        /// <summary>
        /// 消息来源的操作者的租户ID
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// 消息来源的操作者的租户名称
        /// </summary>
        public string TenantName { get; set; }
    }
}