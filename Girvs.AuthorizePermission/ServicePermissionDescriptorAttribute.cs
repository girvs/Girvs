using System;

namespace Girvs.AuthorizePermission
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ServicePermissionDescriptorAttribute : System.Attribute
    {
        public ServicePermissionDescriptorAttribute(string serviceName, string serviceId, string tag = "", int order = 0)
        {
            ServiceId = Guid.Parse(serviceId);
            ServiceName = serviceName;
            Tag = tag;
            Order = order;
        }

        public Guid ServiceId { get; }
        public string ServiceName { get; set; }

        /// <summary>
        /// 所属标签
        /// </summary>
        public string Tag { get; private set; }

        /// <summary>
        /// 排序
        /// </summary>
        public int Order { get; private set; }
    }
}