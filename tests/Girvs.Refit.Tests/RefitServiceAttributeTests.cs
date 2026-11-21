namespace Girvs.Refit.Tests;

public class RefitServiceAttributeTests
{
    [Theory]
    [InlineData(true, RefitServiceAddressType.ServiceDiscovery)]
    [InlineData(false, RefitServiceAddressType.Static)]
    public void 历史inConsul参数映射为地址来源(
        bool inConsul,
        RefitServiceAddressType expected)
    {
        // 验证过时 API 兼容行为：历史 (string, bool) 构造仍映射到地址来源；构造移除时一并删除本测试。
#pragma warning disable CS0618
        var attribute = new RefitServiceAttribute("ordersservice", inConsul);
#pragma warning restore CS0618

        Assert.Equal(expected, attribute.AddressType);
    }
}
