using Girvs.Configuration;
using Girvs.Configuration.Resources;
using Microsoft.Extensions.Configuration;
using GirvsResource = Girvs.Configuration.Resources.Resource;

namespace Girvs.Aspire.Hosting.Tests;

public class ConnectionResourceResolverTests
{
    [Fact]
    public void AppSettings绑定Resources字典()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string>
                {
                    ["Resources:primary-mysql:Type"] = "mysql",
                    ["Resources:primary-mysql:Settings:Host"] = "mysql",
                    ["Resources:primary-mysql:Settings:Port"] = "3306",
                }
            )
            .Build();
        var settings = new AppSettings();

        configuration.Bind(settings);

        var resource = Assert.IsType<GirvsResource>(settings.Resources["primary-mysql"]);
        Assert.Equal("mysql", resource.Type);
        Assert.Equal("mysql", resource.Settings["Host"]);
        Assert.Equal("3306", resource.Settings["Port"]);
    }

    [Fact]
    public void 自定义资源类型保留给消费模块处理()
    {
        var resource = new GirvsResource { Type = "elasticsearch" };

        Assert.Equal("elasticsearch", resource.Type);
    }
}
