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
        public FuncModule FuncModule { get; }
        public string[] OtherParams { get; }

        public ServiceMethodPermissionDescriptorAttribute(string metohodName, Permission permission,
            UserType userType = UserType.All, FuncModule funcModule = FuncModule.All, params string[] otherParams)
        {
            MethodName = metohodName;
            Permission = permission;
            UserType = userType;
            FuncModule = funcModule;
            OtherParams = otherParams;
        }
    }
}