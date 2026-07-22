namespace Girvs.Refit.Tests;

public class RefitConfigTests
{
    [Fact]
    public void 默认配置使用Aspire提供者()
    {
        var config = new RefitConfig();

        Assert.Equal(RefitDiscoveryProvider.Aspire, config.DiscoveryProvider);
    }

    [Fact]
    public void 静态端点优先读取新配置并兼容历史配置()
    {
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

        Assert.Equal("https://new.example", config.GetServiceEndpoint("partner"));
        Assert.Equal("https://old.example", config.GetServiceEndpoint("legacy"));
    }
}
