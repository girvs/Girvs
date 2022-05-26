using System;
using Girvs.Infrastructure;

namespace Girvs.AuthorizePermission
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ServicePermissionDescriptorAttribute : System.Attribute
    {
        public ServicePermissionDescriptorAttribute(string serviceName, string serviceId, string tag = "",
            int order = 0, SystemModule systemModule = SystemModule.All, params string[] otherParams)
        {
            ServiceId = Guid.Parse(serviceId);
            ServiceName = serviceName;
            Tag = tag;
            Order = order;
            SystemModule = systemModule;
            OtherParams = otherParams;
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

        /// <summary>
        /// 所属的子系统模块
        /// </summary>
        public SystemModule SystemModule { get; }

        /// <summary>
        /// 其它相关参数
        /// </summary>
        public string[] OtherParams { get; }
    }
}