using System.Text;
using Girvs.Gateway.FlowProtection;
using Girvs.Gateway.FlowProtection.Configuration;
using Microsoft.Extensions.Configuration;

namespace Girvs.Gateway.Tests;

public class FlowDefinitionRegistryVariantsTests
{
    [Fact]
    public void Json配置绑定_多组合与免前置参数单组合可共存()
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
                    "Variants": { " Ae ": ["a", " E "], "Direct": ["E"] }
                  },
                  {
                    "FlowId": "single",
                    "Interfaces": [
                      { "Name": "A", "Method": "POST", "Path": "/single/start" },
                      { "Name": "B", "Method": "POST", "Path": "/single/end" }
                    ],
                    "Variants": { "default": ["A", "B"] }
                  }
                ]
              }
            }
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var configuration = new ConfigurationBuilder().AddJsonStream(stream).Build();
        var options = configuration.GetRequiredSection("FlowProtection").Get<FlowProtectionOptions>();

        var registry = new FlowDefinitionRegistry(options);

        Assert.True(registry.TryGetEndpoint("post", "/A/", out var endpoint));
        Assert.Equal("X-Mode", endpoint.Flow.ParameterHeaderName);
        Assert.True(endpoint.TryMatchVariant("ae", out var selected));
        Assert.Equal("Ae", selected.VariantKey);
        Assert.True(selected.IsFirstStep);
        Assert.True(registry.TryGetEndpoint("post", "/single/start/", out var single));
        Assert.False(single.Flow.RequiresSelector);
        Assert.True(single.TryMatchVariant(DefaultVariantKey, out var start));
        Assert.True(start.IsFirstStep);
    }

    [Theory]
    [InlineData(null, false, false)]
    [InlineData("  ", false, false)]
    [InlineData(" X-Mode ", false, false)]
    [InlineData(null, true, false)]
    [InlineData(" X-Mode ", true, false)]
    [InlineData(" X-Mode ", true, true)]
    public void 配置绑定_编译单组合及单步组合(string? header, bool singleStep, bool multipleVariants)
    {
        const string flow = "FlowProtection:Flows:0";
        var values = new Dictionary<string, string?>
        {
            [$"{flow}:FlowId"] = "variants",
            [$"{flow}:Interfaces:0:Name"] = "E",
            [$"{flow}:Interfaces:0:Method"] = "POST",
            [$"{flow}:Interfaces:0:Path"] = "/e",
        };
        if (header is not null)
        {
            values[$"{flow}:ParameterHeaderName"] = header;
        }

        if (!singleStep || multipleVariants)
        {
            values[$"{flow}:Interfaces:1:Name"] = "A";
            values[$"{flow}:Interfaces:1:Method"] = "POST";
            values[$"{flow}:Interfaces:1:Path"] = "/a";
            values[$"{flow}:Variants:AE:0"] = "A";
            values[$"{flow}:Variants:AE:1"] = "E";
        }

        if (singleStep)
        {
            values[$"{flow}:Variants: direct :0"] = "E";
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var registry = new FlowDefinitionRegistry(configuration.GetRequiredSection("FlowProtection").Get<FlowProtectionOptions>());

        Assert.True(registry.TryGetEndpoint("POST", "/e", out var endpoint));
        Assert.Equal(header?.Trim() ?? string.Empty, endpoint.Flow.ParameterHeaderName);
        Assert.Equal(header == " X-Mode ", endpoint.Flow.RequiresSelector);
        Assert.Equal(multipleVariants ? 2 : 1, endpoint.Flow.Variants!.Count);
        Assert.True(endpoint.TryMatchVariant(singleStep ? "DIRECT" : "ae", out var match));
        Assert.Equal(singleStep ? "direct" : "AE", match.VariantKey);
        Assert.Equal(singleStep, match.IsFirstStep);
        Assert.True(match.IsLastStep);
        if (multipleVariants)
        {
            Assert.True(endpoint.TryMatchVariant("AE", out var multiStepMatch));
            Assert.False(multiStepMatch.IsFirstStep);
            Assert.True(multiStepMatch.IsLastStep);
        }
    }

    [Theory]
    [InlineData("ADE", 1, 3)]
    [InlineData("de", 0, 2)]
    public void 共享接口D_按组合返回准确顺位(string variant, int index, int count)
    {
        var registry = new FlowDefinitionRegistry(CreateOptions());

        Assert.True(registry.TryGetEndpoint("post", "/D/", out var endpoint));
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

        options.Flows[0].Variants = new() { ["ABCDE"] = ["A", "B", "C", "D", "E"] };
        Assert.Contains("与系统保留头冲突", Assert.Throws<GirvsException>(() => new FlowDefinitionRegistry(options)).Message);
    }

    public static TheoryData<string, string> InvalidDefinitions => new()
    {
        { "空白参数头", "ParameterHeaderName 不能为空" },
        { "空参数头", "ParameterHeaderName 不能为空" },
        { "空字符串参数头", "ParameterHeaderName 不能为空" },
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
        { "空组合集合与空参数头", "Variants 不能为空" },
        { "空白组合键", "组合键不能为空" },
        { "免前置参数的空白组合键", "组合键不能为空" },
        { "大小写重复组合键", "组合键 'abe' 重复" },
        { "带空白重复组合键", "组合键 'ABE' 重复" },
        { "零步组合", "组合 'ABE' 至少需要一个步骤" },
        { "空组合步骤", "组合 'ABE' 至少需要一个步骤" },
        { "空白接口引用", "接口引用不能为空" },
        { "空接口引用", "接口引用不能为空" },
        { "未定义接口引用", "引用了未定义的接口 'unknown'" },
        { "重复接口引用", "重复引用接口 'a'" },
        { "孤立接口", "接口 'F' 未被任何组合引用" },
        { "空流程元素", "Flows 不能包含空项" },
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
            case "空白参数头": flow.ParameterHeaderName = "  "; break;
            case "空参数头": flow.ParameterHeaderName = null; break;
            case "空字符串参数头": flow.ParameterHeaderName = ""; break;
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
            case "空组合集合与空参数头":
                flow.Variants.Clear();
                flow.ParameterHeaderName = "";
                break;
            case "空白组合键": flow.Variants.Add(" ", ["A", "E"]); break;
            case "免前置参数的空白组合键":
                flow.Variants = new() { [" "] = ["A", "B", "C", "D", "E"] };
                flow.ParameterHeaderName = "";
                break;
            case "大小写重复组合键": flow.Variants.Add("abe", ["A", "E"]); break;
            case "带空白重复组合键": flow.Variants.Add(" ABE ", ["A", "E"]); break;
            case "零步组合": flow.Variants["ABE"] = []; break;
            case "空组合步骤": flow.Variants["ABE"] = null; break;
            case "空白接口引用": flow.Variants["ABE"][0] = " "; break;
            case "空接口引用": flow.Variants["ABE"][0] = null; break;
            case "未定义接口引用": flow.Variants["ABE"][0] = " unknown "; break;
            case "重复接口引用": flow.Variants["ABE"][1] = " a "; break;
            case "孤立接口": flow.Interfaces.Add(new FlowInterfaceOptions { Name = "F", Method = "POST", Path = "/f" }); break;
            case "空流程元素": options.Flows[0] = null; break;
            case "非法流程TTL": flow.TtlSeconds = 1801; break;
            default: throw new ArgumentOutOfRangeException(nameof(scenario));
        }

        Assert.Contains(message, Assert.Throws<GirvsException>(() => new FlowDefinitionRegistry(options)).Message);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public void 跨流程重复方法与路径_启动失败(bool multipleVariantsFirst, bool multipleVariantsSecond)
    {
        var first = multipleVariantsFirst ? CreateMultipleVariants() : CreateSingleVariant("first", "/A/", "/first/end");
        var second = multipleVariantsSecond ? CreateMultipleVariants() : CreateSingleVariant("second", "/a", "/second/end");
        first.FlowId = "first";
        second.FlowId = "second";
        var options = new FlowProtectionOptions { Flows = [first, second] };

        Assert.Contains("被多个流程重复使用", Assert.Throws<GirvsException>(() => new FlowDefinitionRegistry(options)).Message);
    }

    internal static FlowProtectionOptions CreateOptions() => new()
    {
        Enabled = true,
        Flows = [CreateMultipleVariants(), CreateSingleVariant("single", "/single/start", "/single/end")],
    };

    private static FlowDefinitionOptions CreateMultipleVariants() => new()
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

    internal const string DefaultVariantKey = "default";

    internal static FlowDefinitionOptions CreateSingleVariant(string flowId, params string[] paths)
    {
        var interfaces = paths.Select((path, index) => new FlowInterfaceOptions
        {
            Name = $"endpoint{index}",
            Method = "POST",
            Path = path,
        }).ToList();

        return new FlowDefinitionOptions
        {
            FlowId = flowId,
            Interfaces = interfaces,
            Variants = new() { [DefaultVariantKey] = interfaces.Select(endpoint => endpoint.Name).ToList() },
        };
    }
}
