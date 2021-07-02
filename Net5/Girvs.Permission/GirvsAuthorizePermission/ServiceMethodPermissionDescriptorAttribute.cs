using System;

namespace Girvs.Domain.GirvsAuthorizePermission
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ServiceMethodPermissionDescriptorAttribute : System.Attribute
    {
        public string MethodName { get; }
        public Permission Permission { get; }

        public ServiceMethodPermissionDescriptorAttribute(string metohodName, Permission permission)
        {
            MethodName = metohodName;
            Permission = permission;
        }
    }
}