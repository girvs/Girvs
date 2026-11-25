using System.Text;
using System.Text.Json;
using Girvs.Gateway.FlowProtection;
using Girvs.Gateway.FlowProtection.Configuration;
using Microsoft.Extensions.Configuration;

namespace Girvs.Gateway.Tests;

public class FlowDefinitionRegistryVariantsTests
{
    [Fact]
    public void Json配置绑定_多组合和线性流程可共存()
    {
        const string json = """
            {
              "FlowProtection": {
                "Flows": [
                  {
                    "FlowId": "variants",
                    "ParameterHeaderName": " X-Mode ",
                    "Interfaces": [
                      { "Name": " A ", "Method": " post ", "Path": "/a/" },
                      { "Name": "E", "Method": "POST", "Path": "/e" }
                    ],
                    "Variants": { " Ae ": ["a", " E "] }
                  },
                  {
                    "FlowId": "linear",
                    "Steps": [
                      { "Method": "POST", "Path": "/linear/start" },
                      { "Method": "POST", "Path": "/linear/end" }
                    ]
                  }
                ]
              }
            }
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var configuration = new ConfigurationBuilder().AddJsonStream(stream).Build();
        var options = configuration.GetRequiredSection("FlowProtection").Get<FlowProtectionOptions>();

        var registry = new FlowDefinitionRegistry(options);

        Assert.False(registry.TryGetStep("post", "/A/", out _));
        Assert.True(registry.TryGetEndpoint("post", "/A/", out var endpoint));
        Assert.Equal(FlowDefinitionKind.Parameterized, endpoint.Flow.Kind);
        Assert.Equal("X-Mode", endpoint.Flow.ParameterHeaderName);
        Assert.True(endpoint.TryMatchVariant("ae", out var selected));
        Assert.Equal("Ae", selected.VariantKey);
        Assert.True(selected.IsFirstStep);
        Assert.True(registry.TryGetStep("post", "/linear/start/", out var start));
        Assert.Equal(FlowDefinitionKind.Linear, start.Flow.Kind);
        Assert.True(start.IsFirstStep);
    }

    [Fact]
    public void 存量记录的构造与解构及Json反序列化_保留线性默认值()
    {
        var definition = new FlowDefinition("linear", 300, Array.Empty<FlowStepDefinition>());
        var (flowId, ttl, steps) = definition;
        var request = new FlowReserveRequest("ticket", "linear", "biz", 0, "attempt", 300, 30);
        var (ticket, requestFlowId, businessId, expectedIndex, attemptId, flowTtl, lockTtl) = request;

        Assert.Equal(("linear", 300), (flowId, ttl));
        Assert.Empty(steps);
        Assert.Equal(("ticket", "linear", "biz", 0, "attempt", 300, 30),
            (ticket, requestFlowId, businessId, expectedIndex, attemptId, flowTtl, lockTtl));

        var legacyDefinition = JsonSerializer.Deserialize<FlowDefinition>(
            """{"FlowId":"linear","TtlSeconds":300,"Steps":[]}"""
        );
        var legacyRequest = JsonSerializer.Deserialize<FlowReserveRequest>(
            """{"Ticket":"ticket","FlowId":"linear","BusinessId":"biz","ExpectedIndex":0,"AttemptId":"attempt","FlowTtlSeconds":300,"LockTtlSeconds":30}"""
        );
        Assert.Equal(FlowDefinitionKind.Linear, legacyDefinition.Kind);
        Assert.Empty(legacyDefinition.ParameterHeaderName);
        Assert.Null(legacyDefinition.Variants);
        Assert.Equal(request, legacyRequest);
    }

    [Theory]
    [InlineData("ADE", 1, 3)]
    [InlineData("de", 0, 2)]
    public void 共享接口D_按组合返回准确顺位(string variant, int index, int count)
    {
        var registry = new FlowDefinitionRegistry(CreateOptions());

        Assert.True(registry.TryGetEndpoint("post", "/D/", out var endpoint));
        Assert.Null(endpoint.LinearMatch);
        Assert.False(endpoint.TryMatchVariant("ABE", out _));
        Assert.True(endpoint.TryMatchVariant(variant, out var match));
        Assert.Equal(variant.ToUpperInvariant(), match.VariantKey);
        Assert.Equal(index, match.StepIndex);
        Assert.Equal(count, match.VariantStepCount);
        Assert.False(match.IsLastStep);
    }

    [Theory]
    [InlineData("ABE", 2)]
    [InlineData("ACE", 2)]
    [InlineData("ADE", 2)]
    [InlineData("DE", 1)]
    public void 共享接口E_在各组合中均为最后一步(string variant, int index)
    {
        var registry = new FlowDefinitionRegistry(CreateOptions());

        Assert.True(registry.TryGetEndpoint("POST", "/e", out var endpoint));
        Assert.True(endpoint.TryMatchVariant(variant, out var match));
        Assert.Equal(index, match.StepIndex);
        Assert.True(match.IsLastStep);
    }

    [Theory]
    [InlineData(" x-custom-ticket ")]
    [InlineData(" x-custom-business ")]
    [InlineData(" x-flow-next-index ")]
    [InlineData(" x-flow-completed ")]
    [InlineData(" x-flow-business-id ")]
    public void 前置参数头与保留头冲突_启动失败(string header)
    {
        var options = CreateOptions();
        options.TicketHeaderName = " X-Custom-Ticket ";
        options.BusinessIdHeaderName = " X-Custom-Business ";
        options.Flows[0].ParameterHeaderName = header;

        Assert.Contains("与系统保留头冲突", Assert.Throws<GirvsException>(() => new FlowDefinitionRegistry(options)).Message);
    }

    public static TheoryData<string, string> InvalidDefinitions => new()
    {
        { "混配", "不能同时配置 Steps" },
        { "空白参数头", "ParameterHeaderName 不能为空" },
        { "空参数头", "ParameterHeaderName 不能为空" },
        { "空接口集合", "Interfaces 不能为空" },
        { "缺失接口集合", "Interfaces 不能为空" },
        { "空接口元素", "Interfaces 不能包含空项" },
        { "空白接口名", "Name 不能为空" },
        { "接口重名", "接口名称 'a' 重复" },
        { "接口重路径", "接口 'POST /A' 重复" },
        { "非法方法", "不是合法 HTTP Method" },
        { "非法路径", "Path 必须以 / 开始" },
        { "空组合集合", "Variants 不能为空" },
        { "缺失组合集合", "Variants 不能为空" },
        { "空白组合键", "组合键不能为空" },
        { "大小写重复组合键", "组合键 'abe' 重复" },
        { "带空白重复组合键", "组合键 'ABE' 重复" },
        { "单步组合", "组合 'ABE' 至少需要两个步骤" },
        { "空组合步骤", "组合 'ABE' 至少需要两个步骤" },
        { "空白接口引用", "接口引用不能为空" },
        { "空接口引用", "接口引用不能为空" },
        { "未定义接口引用", "引用了未定义的接口 'unknown'" },
        { "重复接口引用", "重复引用接口 'a'" },
        { "孤立接口", "接口 'F' 未被任何组合引用" },
        { "空流程元素", "Flows 不能包含空项" },
        { "空线性步骤元素", "Steps 不能包含空项" },
        { "非法流程TTL", "TtlSeconds 必须在 1 到 1800 秒之间" },
    };

    [Theory]
    [MemberData(nameof(InvalidDefinitions))]
    public void 非法配置_启动时抛出GirvsException(string scenario, string message)
    {
        var options = CreateOptions();
        var flow = options.Flows[0];
        switch (scenario)
        {
            case "混配": flow.Steps = CreateLinear().Steps; break;
            case "空白参数头": flow.ParameterHeaderName = "  "; break;
            case "空参数头": flow.ParameterHeaderName = null; break;
            case "空接口集合": flow.Interfaces.Clear(); break;
            case "缺失接口集合": flow.Interfaces = null; break;
            case "空接口元素": flow.Interfaces[0] = null; break;
            case "空白接口名": flow.Interfaces[0].Name = " "; break;
            case "接口重名": flow.Interfaces[1].Name = " a "; break;
            case "接口重路径": flow.Interfaces[1].Path = "/A/"; break;
            case "非法方法": flow.Interfaces[0].Method = "INVALID"; break;
            case "非法路径": flow.Interfaces[0].Path = "api/a"; break;
            case "空组合集合": flow.Variants.Clear(); break;
            case "缺失组合集合": flow.Variants = null; break;
            case "空白组合键": flow.Variants.Add(" ", ["A", "E"]); break;
            case "大小写重复组合键": flow.Variants.Add("abe", ["A", "E"]); break;
            case "带空白重复组合键": flow.Variants.Add(" ABE ", ["A", "E"]); break;
            case "单步组合": flow.Variants["ABE"] = ["A"]; break;
            case "空组合步骤": flow.Variants["ABE"] = null; break;
            case "空白接口引用": flow.Variants["ABE"][0] = " "; break;
            case "空接口引用": flow.Variants["ABE"][0] = null; break;
            case "未定义接口引用": flow.Variants["ABE"][0] = " unknown "; break;
            case "重复接口引用": flow.Variants["ABE"][1] = " a "; break;
            case "孤立接口": flow.Interfaces.Add(new FlowInterfaceOptions { Name = "F", Method = "POST", Path = "/f" }); break;
            case "空流程元素": options.Flows[0] = null; break;
            case "空线性步骤元素": options.Flows[1].Steps[0] = null; break;
            case "非法流程TTL": flow.TtlSeconds = 1801; break;
            default: throw new ArgumentOutOfRangeException(nameof(scenario));
        }

        Assert.Contains(message, Assert.Throws<GirvsException>(() => new FlowDefinitionRegistry(options)).Message);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void 跨流程重复方法与路径_启动失败(bool parameterizedFirst, bool parameterizedSecond)
    {
        var first = parameterizedFirst ? CreateParameterized() : CreateLinear();
        var second = parameterizedSecond ? CreateParameterized() : CreateLinear();
        first.FlowId = "first";
        second.FlowId = "second";
        if (!parameterizedFirst)
        {
            first.Steps[0].Path = "/A/";
        }

        if (!parameterizedSecond)
        {
            second.Steps[0].Path = "/A/";
        }

        var options = new FlowProtectionOptions { Flows = [first, second] };

        Assert.Contains("被多个流程重复使用", Assert.Throws<GirvsException>(() => new FlowDefinitionRegistry(options)).Message);
    }

    internal static FlowProtectionOptions CreateOptions() => new()
    {
        Enabled = true,
        Flows = [CreateParameterized(), CreateLinear()],
    };

    private static FlowDefinitionOptions CreateParameterized() => new()
    {
        FlowId = "variants",
        ParameterHeaderName = "X-Mode",
        Interfaces =
        [
            new() { Name = "A", Method = "POST", Path = "/a" },
            new() { Name = "B", Method = "POST", Path = "/b" },
            new() { Name = "C", Method = "POST", Path = "/c" },
            new() { Name = "D", Method = "POST", Path = "/d" },
            new() { Name = "E", Method = "POST", Path = "/e" },
        ],
        Variants = new Dictionary<string, List<string>>
        {
            ["ABE"] = ["A", "B", "E"],
            ["ACE"] = ["A", "C", "E"],
            ["ADE"] = ["A", "D", "E"],
            ["DE"] = ["D", "E"],
        },
    };

    private static FlowDefinitionOptions CreateLinear() => new()
    {
        FlowId = "linear",
        Steps =
        [
            new() { Method = "POST", Path = "/linear/start" },
            new() { Method = "POST", Path = "/linear/end" },
        ],
    };
}
