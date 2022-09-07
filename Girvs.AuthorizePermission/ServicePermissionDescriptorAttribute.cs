namespace Girvs.AuthorizePermission;

[AttributeUsage(AttributeTargets.Class)]
public class ServicePermissionDescriptorAttribute : System.Attribute
{
    // public ServicePermissionDescriptorAttribute(string serviceName, string serviceId, string tag = "",
    //     int order = 0, SystemModule systemModule = SystemModule.All, params string[] otherParams)
    // {
    //     ServiceId = Guid.Parse(serviceId);
    //     ServiceName = serviceName;
    //     Tag = tag;
    //     Order = order;
    //     SystemModule = systemModule;
    //     OtherParams = otherParams;
    // }

    /// <summary>
    /// 功能菜单权限
    /// </summary>
    /// <param name="serviceName">服务名称</param>
    /// <param name="serviceId">服务Id</param>
    /// <param name="tag">所属标签</param>
    /// <param name="systemModule">所属模块</param>
    /// <param name="order">排序</param>
    /// <param name="otherParams">其它参数</param>
    public ServicePermissionDescriptorAttribute(string serviceName, string serviceId, string tag,
        SystemModule systemModule = SystemModule.All,int order = 0,  params string[] otherParams)
    {
        ServiceId = Guid.Parse(serviceId);
        ServiceName = serviceName;
        Tag = tag;
        SystemModule = systemModule;
        Order = order;
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