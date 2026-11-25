using Girvs.Gateway.FlowProtection;
using Girvs.Gateway.FlowProtection.Configuration;

namespace Girvs.Gateway.Tests;

public class FlowDefinitionRegistryTemplateTests
{
    [Theory]
    [InlineData("linear", "/orders/{id}", "/ORDERS/{Id}", "重复", null)]
    [InlineData("linear", "/orders/{id}", "/orders/{orderId}", "同形异名冲突", null)]
    [InlineData("linear", "/orders/{id}/items", "/orders/export/{type}", "交叉重叠冲突", "/orders/export/items")]
    [InlineData("pool", "/orders/{id}", "/ORDERS/{Id}", "重复", null)]
    [InlineData("pool", "/orders/{id}", "/orders/{orderId}", "同形异名冲突", null)]
    [InlineData("pool", "/orders/{id}/items", "/orders/export/{type}", "交叉重叠冲突", "/orders/export/items")]
    [InlineData("cross", "/orders/{id}", "/ORDERS/{Id}", "重复", null)]
    [InlineData("cross", "/orders/{id}", "/orders/{orderId}", "同形异名冲突", null)]
    [InlineData("cross", "/orders/{tenant}/{id}/items", "/orders/{region}/export/{type}", "交叉重叠冲突", "/orders/value/export/items")]
    public void 模板冲突_按归属和类别报告(string scope, string first, string second, string category, string? witness)
    {
        var flows = scope switch
        {
            "linear" => new[] { Linear("local", first, second) },
            "pool" => [Parameterized("local", first, second)],
            _ => [Linear("left", first, "/left/end"), Parameterized("right", second, "/right/end")],
        };

        var message = Assert.Throws<GirvsException>(() => CreateRegistry(flows)).Message;

        Assert.Contains(category, message);
        Assert.Contains(first, message);
        Assert.Contains(second, message);
        if (witness is not null)
        {
            Assert.Contains($"见证路径 '{witness}'", message);
        }

        switch (scope)
        {
            case "linear":
                Assert.Contains("流程 'local' 中的线性步骤", message);
                Assert.DoesNotContain("跨流程", message);
                break;
            case "pool":
                Assert.Contains("流程 'local' 的接口池", message);
                Assert.DoesNotContain("跨流程", message);
                break;
            default:
                Assert.Contains("跨流程", message);
                Assert.Contains("'left'", message);
                Assert.Contains("'right'", message);
                break;
        }
    }

    [Theory]
    [InlineData("linear")]
    [InlineData("pool")]
    [InlineData("cross")]
    public void 静态路径与模板并存_始终命中静态路径(string scope)
    {
        var flows = scope switch
        {
            "linear" => new[] { Linear("local", "/orders/{id}", "/orders/create") },
            "pool" => [Parameterized("local", "/orders/{id}", "/orders/create")],
            _ => [Linear("template", "/orders/{id}", "/template/end"), Parameterized("static", "/orders/create", "/static/end")],
        };
        var registry = CreateRegistry(flows);

        Assert.True(registry.TryGetEndpoint("post", "/ORDERS/Create/", out var staticMatch));
        Assert.Equal("/orders/create", staticMatch.Step.Path.Value);
        Assert.True(registry.TryGetEndpoint("POST", "/orders/123", out var templateMatch));
        Assert.Equal("/orders/{id}", templateMatch.Step.Path.Value);
        Assert.False(registry.TryGetEndpoint("POST", "/orders/create//", out _));
    }

    [Fact]
    public void 模板有字面区分位或不同的方法段数_分别命中所属接口()
    {
        var alternateMethod = Linear("method", "/a/{id}/x", "/method/end");
        alternateMethod.Steps[0].Method = "GET";
        var registry = CreateRegistry(
            Linear("left", "/a/{id}/x", "/left/end"),
            Parameterized("right", "/a/{id2}/Y", "/right/end"),
            Linear("segments", "/a/{id}", "/segments/end"),
            alternateMethod
        );

        Assert.True(registry.TryGetEndpoint("POST", "/a/1/x", out var left));
        Assert.Equal("left", left.Flow.FlowId);
        Assert.True(registry.TryGetEndpoint("pOsT", "/a/1/y", out var right));
        Assert.Equal("right", right.Flow.FlowId);
        Assert.True(right.TryMatchVariant("AB", out var variantStep));
        Assert.True(variantStep.IsFirstStep);
        Assert.True(registry.TryGetEndpoint("POST", "/a/1", out var segments));
        Assert.Equal("segments", segments.Flow.FlowId);
        Assert.True(registry.TryGetEndpoint("get", "/a/1/x", out var method));
        Assert.Equal("method", method.Flow.FlowId);
        Assert.False(registry.TryGetEndpoint("POST", "/a/1/z", out _));
    }

    internal static FlowDefinitionOptions Linear(string flowId, string first, string second) => new()
    {
        FlowId = flowId,
        Steps =
        [
            new FlowStepOptions { Method = "POST", Path = first },
            new FlowStepOptions { Method = "POST", Path = second },
        ],
    };

    internal static FlowDefinitionOptions Parameterized(string flowId, string first, string second) => new()
    {
        FlowId = flowId,
        ParameterHeaderName = "X-Mode",
        Interfaces =
        [
            new FlowInterfaceOptions { Name = "A", Method = "POST", Path = first },
            new FlowInterfaceOptions { Name = "B", Method = "POST", Path = second },
        ],
        Variants = new() { ["AB"] = ["A", "B"] },
    };

    private static FlowDefinitionRegistry CreateRegistry(params FlowDefinitionOptions[] flows) =>
        new(new FlowProtectionOptions { Flows = [.. flows] });
}
