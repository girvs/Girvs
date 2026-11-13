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
        var attribute = new RefitServiceAttribute("ordersservice", inConsul);

        Assert.Equal(expected, attribute.AddressType);
    }
}
