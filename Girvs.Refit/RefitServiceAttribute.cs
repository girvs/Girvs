namespace Girvs.Refit;

public enum RefitServiceAddressType
{
    Static,
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
    public RefitServiceAttribute(string serviceName, bool inConsul)
        : this(
            serviceName,
            inConsul ? RefitServiceAddressType.ServiceDiscovery : RefitServiceAddressType.Static) { }

    public string ServiceName { get; }

    public RefitServiceAddressType AddressType { get; }

    [Obsolete("请使用 AddressType")]
    public bool InConsul => AddressType == RefitServiceAddressType.ServiceDiscovery;
}
