using Girvs.Gateway.FlowProtection;
using Girvs.Gateway.FlowProtection.Configuration;

namespace Girvs.Gateway.Tests;

public class FlowPathTemplateTests
{
    [Theory]
    [InlineData("  /Orders/{_order0}/{Line_2}/  ", "/orders/ABC/42/", "/Orders/{_order0}/{Line_2}")]
    [InlineData("/{id}", "/42", "/{id}")]
    [InlineData(" / ", "/", "/")]
    [InlineData("/Orders/", "/orders/", "/Orders")]
    public void 合法配置_按规范化路径匹配(string configuredPath, string requestPath, string expectedPath)
    {
        var registry = CreateRegistry(configuredPath);

        Assert.True(registry.TryGetStep("post", requestPath, out var match));
        Assert.Equal(expectedPath, match.Step.Path.Value);
        Assert.True(match.IsFirstStep);
    }

    [Theory]
    [InlineData(null, "Path 不能为空")]
    [InlineData("", "Path 不能为空")]
    [InlineData("  ", "Path 不能为空")]
    [InlineData("orders/{id}", "Path 必须以 / 开始")]
    [InlineData("//", "空路径段")]
    [InlineData("/orders//items", "空路径段")]
    [InlineData("/orders//", "空路径段")]
    [InlineData("/orders/{id}//", "空路径段")]
    [InlineData("/orders/*", "非法字符")]
    [InlineData("/orders?q=x", "非法字符")]
    [InlineData("/orders#x", "非法字符")]
    [InlineData("/orders/{id}/*", "非法字符")]
    [InlineData("/orders/{id}?", "非法字符")]
    [InlineData("/orders/{id}#", "非法字符")]
    [InlineData("/orders/v{ver}", "非法参数段")]
    [InlineData("/orders/{id}x", "非法参数段")]
    [InlineData("/orders/{a}{b}", "非法参数段")]
    [InlineData("/orders/{id", "非法参数段")]
    [InlineData("/orders/id}", "非法参数段")]
    [InlineData("/orders/{}", "非法参数段")]
    [InlineData("/orders/{id:int}", "非法参数段")]
    [InlineData("/orders/{1id}", "非法参数段")]
    [InlineData("/orders/{a-b}", "非法参数段")]
    [InlineData("/orders/{订单}", "非法参数段")]
    [InlineData("/orders/{ id}", "非法参数段")]
    [InlineData("/a/{id}/b/{id}", "参数名 'id' 重复")]
    [InlineData("/a/{id}/b/{Id}", "参数名 'Id' 重复")]
    public void 非法配置_启动失败(string? configuredPath, string reason)
    {
        var exception = Assert.Throws<GirvsException>(() => CreateRegistry(configuredPath));

        Assert.Contains("'flow'", exception.Message);
        Assert.Contains(reason, exception.Message);
    }

    [Theory]
    [InlineData("POST", null, false)]
    [InlineData("POST", "", false)]
    [InlineData(null, "/orders/123", false)]
    [InlineData("", "/orders/123", false)]
    [InlineData("  ", "/orders/123", false)]
    [InlineData("GET", "/orders/123", false)]
    [InlineData("POST", "/orders", false)]
    [InlineData("POST", "/orders//", false)]
    [InlineData("POST", "/orders/123//", false)]
    [InlineData("POST", "/orders/123/items", false)]
    [InlineData("POST", "/orders/{raw}", true)]
    [InlineData("POST", "/orders/*?#", true)]
    [InlineData("POST", "/other/*?#", false)]
    [InlineData("post", "/Orders/123/", true)]
    public void 请求路径_按非空整段匹配且异常输入安全返回(string? method, string? requestPath, bool expected)
    {
        var registry = CreateRegistry("/orders/{id}");

        Assert.Equal(expected, registry.TryGetEndpoint(method!, new PathString(requestPath), out var match));
        Assert.Equal(expected, match is not null);
    }

    [Fact]
    public void 请求路径含中间空段_同段数也不匹配参数()
    {
        var registry = CreateRegistry("/orders/{id}/items");

        Assert.False(registry.TryGetEndpoint("POST", "/orders//items", out _));
    }

    [Fact]
    public void 请求路径缺少开头斜杠_返回未命中()
    {
        // PathString 的构造函数会先拒绝该输入，直接构造字段值以覆盖注册表的防御分支。
        var raw = "orders/123";
        var path = System.Runtime.CompilerServices.Unsafe.As<string, PathString>(ref raw);

        Assert.False(CreateRegistry("/orders/{id}").TryGetEndpoint("POST", path, out var match));
        Assert.Null(match);
    }

    [Fact]
    public void 非法请求方法不能用空格拼接命中静态路径()
    {
        var registry = CreateRegistry("/orders /confirm");

        Assert.True(registry.TryGetEndpoint("POST", "/orders /confirm", out _));
        Assert.False(registry.TryGetEndpoint("POST /orders", "/confirm", out _));
    }

    [Fact]
    public void 根路径是没有参数和路径段的静态路径()
    {
        var root = FlowPathTemplate.Parse(" / ", "flow");

        Assert.False(root.HasParameter);
        Assert.Equal(0, root.SegmentCount);
        Assert.True(root.Matches("/"));
    }

    private static FlowDefinitionRegistry CreateRegistry(string? path) => new(new FlowProtectionOptions
    {
        Flows =
        [
            new FlowDefinitionOptions
            {
                FlowId = "flow",
                Steps =
                [
                    new FlowStepOptions { Method = "POST", Path = path! },
                    new FlowStepOptions { Method = "POST", Path = "/other/static" },
                ],
            },
        ],
    });
}
