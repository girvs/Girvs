namespace Girvs.Refit;

public enum RefitServiceAddressType
{
    // 调用第三方或遗留远程服务，地址必须由 ServiceEndpoints 配置提供。
    Static,

    // 调用内部服务，具体使用 Consul 还是 Aspire 由部署配置决定。
    ServiceDiscovery
}

public class RefitServiceAttribute : Attribute
{
    public RefitServiceAttribute(
        string serviceName,
        RefitServiceAddressType addressType = RefitServiceAddressType.ServiceDiscovery)
    {
        ServiceName = serviceName;
        AddressType = addressType;
    }

    [Obsolete("请使用 RefitServiceAddressType 指定接口地址来源")]
    // 保留历史接口的编译兼容性；bool 不再表示具体的发现提供者。
    public RefitServiceAttribute(string serviceName, bool inConsul)
        : this(
            serviceName,
            inConsul ? RefitServiceAddressType.ServiceDiscovery : RefitServiceAddressType.Static) { }

    public string ServiceName { get; }

    public RefitServiceAddressType AddressType { get; }

    [Obsolete("请使用 AddressType")]
    // 供未升级的业务代码读取，内部实现不再依赖该属性选择 Consul。
    public bool InConsul => AddressType == RefitServiceAddressType.ServiceDiscovery;
}
