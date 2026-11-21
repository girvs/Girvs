namespace Girvs.Refit.Tests;

public class RefitConfigTests
{
    [Fact]
    public void 默认配置使用Aspire提供者()
    {
        var config = new RefitConfig();

        // 验证过时 API 兼容行为：DiscoveryProvider 默认值仍为 Aspire；属性移除时一并删除本测试。
#pragma warning disable CS0618
        Assert.Equal(RefitDiscoveryProvider.Aspire, config.DiscoveryProvider);
#pragma warning restore CS0618
    }

    [Fact]
    public void 静态端点优先读取新配置并兼容历史配置()
    {
        // 验证过时 API 兼容行为：历史 ServiceAddress 仍可回退读取；属性移除时一并删除本测试。
#pragma warning disable CS0618
        var config = new RefitConfig
        {
            ServiceEndpoints = new Dictionary<string, string>
            {
                ["partner"] = "https://new.example"
            },
            ServiceAddress = new Dictionary<string, string>
            {
                ["legacy"] = "https://old.example"
            }
        };
#pragma warning restore CS0618

        Assert.Equal("https://new.example", config.GetServiceEndpoint("partner"));
        Assert.Equal("https://old.example", config.GetServiceEndpoint("legacy"));
    }
}
