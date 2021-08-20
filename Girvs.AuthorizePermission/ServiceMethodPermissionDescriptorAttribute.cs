using System;
using Girvs.AuthorizePermission.Enumerations;

namespace Girvs.AuthorizePermission
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ServiceMethodPermissionDescriptorAttribute : Attribute
    {
        public string MethodName { get; }
        public Permission Permission { get; }

        public UserType UserType { get; set; }

        public ServiceMethodPermissionDescriptorAttribute(string metohodName, Permission permission,
            UserType userType = UserType.All)
        {
            MethodName = metohodName;
            Permission = permission;
            UserType = userType;
        }
    }
}