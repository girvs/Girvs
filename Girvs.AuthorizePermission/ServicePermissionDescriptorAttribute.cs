using System;

namespace Girvs.AuthorizePermission
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ServicePermissionDescriptorAttribute : System.Attribute
    {
        public ServicePermissionDescriptorAttribute(string serviceName, string serviceId)
        {
            ServiceId = Guid.Parse(serviceId);
            ServiceName = serviceName;
        }

        public Guid ServiceId { get; }
        public string ServiceName { get; set; }
    }
}