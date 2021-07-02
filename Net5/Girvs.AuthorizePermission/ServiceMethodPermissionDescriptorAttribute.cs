using System;
using Girvs.AuthorizePermission.Enumerations;

namespace Girvs.AuthorizePermission
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