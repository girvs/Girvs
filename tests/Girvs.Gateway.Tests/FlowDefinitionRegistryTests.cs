using Girvs.Gateway.FlowProtection;
using Girvs.Gateway.FlowProtection.Configuration;
using static Girvs.Gateway.Tests.FlowDefinitionRegistryVariantsTests;

namespace Girvs.Gateway.Tests;

public class FlowDefinitionRegistryTests
{
    [Theory]
    [InlineData("", "POST", "/api/a")]
    [InlineData("f1", "BAD METHOD", "/api/a")]
    [InlineData("f1", "POST", "api/a")]
    public void 非法步骤_构造注册表时抛出配置异常(string flowId, string method, string path)
    {
        Assert.Throws<GirvsException>(
            () => new FlowDefinitionRegistry(TestFlows.Create(flowId, method, path, "POST", "/api/b"))
        );
    }

    [Fact]
    public void 不同流程重复MethodPath_构造注册表时抛出配置异常()
    {
        Assert.Throws<GirvsException>(() => new FlowDefinitionRegistry(TestFlows.CreateWithDuplicateStep()));
    }

    [Fact]
    public void 同一流程重复MethodPath_构造注册表时抛出配置异常()
    {
        var options = TestFlows.Create("f1", "POST", "/api/a", "POST", "/api/a");

        Assert.Throws<GirvsException>(() => new FlowDefinitionRegistry(options));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1801)]
    public void 非法Ttl_构造注册表时抛出配置异常(int ttlSeconds)
    {
        var options = TestFlows.Create("f1", "POST", "/api/a", "POST", "/api/b");
        options.Flows[0].TtlSeconds = ttlSeconds;

        Assert.Throws<GirvsException>(() => new FlowDefinitionRegistry(options));
    }

    [Fact]
    public void 合法配置_可按Method和Path忽略大小写匹配步骤()
    {
        var registry = new FlowDefinitionRegistry(
            TestFlows.Create("f1", "POST", "/api/user1/", "GET", "/api/list2")
        );

        Assert.True(registry.TryGetEndpoint("post", "/API/USER1", out var endpoint));
        Assert.True(endpoint.TryMatchVariant("DEFAULT", out var match));
        Assert.Equal(DefaultVariantKey, match.VariantKey);
        Assert.Equal("f1", match.Flow.FlowId);
        Assert.Equal(0, match.StepIndex);
        Assert.Equal("/api/user1", match.Step.Path.Value);
    }

    [Fact]
    public void 未匹配步骤_返回False()
    {
        var registry = new FlowDefinitionRegistry(TestFlows.Create("f1", "POST", "/api/a", "POST", "/api/b"));

        Assert.False(registry.TryGetEndpoint("GET", "/api/none", out _));
    }

    private static class TestFlows
    {
        public static FlowProtectionOptions Create(
            string flowId,
            string firstMethod,
            string firstPath,
            string secondMethod,
            string secondPath
        )
        {
            var flow = CreateSingleVariant(flowId, firstPath, secondPath);
            flow.Interfaces[0].Method = firstMethod;
            flow.Interfaces[1].Method = secondMethod;
            return new FlowProtectionOptions { Enabled = true, Flows = [flow] };
        }

        public static FlowProtectionOptions CreateWithDuplicateStep()
        {
            return new FlowProtectionOptions
            {
                Enabled = true,
                Flows =
                [
                    CreateSingleVariant("f1", "/api/a", "/api/b"),
                    CreateSingleVariant("f2", "/api/a", "/api/c"),
                ],
            };
        }
    }
}
