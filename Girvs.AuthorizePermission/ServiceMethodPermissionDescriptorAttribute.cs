namespace Girvs.AuthorizePermission;

[AttributeUsage(AttributeTargets.Method)]
public class ServiceMethodPermissionDescriptorAttribute : Attribute
{
    public string MethodName { get; }
    public Permission Permission { get; }

    public UserType UserType { get; set; }
    public SystemModule SystemModule { get; }
    public string[] OtherParams { get; }

    public ServiceMethodPermissionDescriptorAttribute(string metohodName, Permission permission,
        UserType userType = UserType.All, SystemModule systemModule = SystemModule.All, params string[] otherParams)
    {
        MethodName = metohodName;
        Permission = permission;
        UserType = userType;
        SystemModule = systemModule;
        OtherParams = otherParams;
    }
}