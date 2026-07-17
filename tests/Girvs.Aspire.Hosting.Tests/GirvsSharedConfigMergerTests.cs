using System.Text.Json;
using GirvsResource = Girvs.Configuration.Resources.Resource;

namespace Girvs.Aspire.Hosting.Tests;

public class GirvsSharedConfigMergerTests
{
    private static KeyValuePair<string, GirvsResource> Entry(
        string name, string type, params (string Key, string Value)[] settings) =>
        new(name, new GirvsResource
        {
            Type = type,
            Settings = settings.ToDictionary(x => x.Key, x => x.Value),
        });

    [Fact]
    public void 无手写文件时生成仅含登记资源的文档()
    {
        var json = GirvsSharedConfigMerger.Merge(null,
            [Entry("platform-redis", "redis", ("Endpoints", "localhost:56379"))]);

        using var doc = JsonDocument.Parse(json);
        var resource = doc.RootElement.GetProperty("Resources").GetProperty("platform-redis");
        Assert.Equal("redis", resource.GetProperty("Type").GetString());
        Assert.Equal("localhost:56379",
            resource.GetProperty("Settings").GetProperty("Endpoints").GetString());
    }

    [Fact]
    public void 手写文件的非Resources节点原样保留()
    {
        const string handWritten =
            """{"Logging":{"LogLevel":{"Default":"Warning"}},"Resources":{}}""";

        var json = GirvsSharedConfigMerger.Merge(handWritten,
            [Entry("platform-redis", "redis", ("Endpoints", "localhost:56379"))]);

        using var doc = JsonDocument.Parse(json);
        Assert.Equal("Warning", doc.RootElement
            .GetProperty("Logging").GetProperty("LogLevel").GetProperty("Default").GetString());
        Assert.True(doc.RootElement.GetProperty("Resources").TryGetProperty("platform-redis", out _));
    }

    [Fact]
    public void 登记资源覆盖手写文件中的同名资源()
    {
        const string handWritten =
            """{"Resources":{"platform-redis":{"Type":"redis","Settings":{"Endpoints":"prod:6379"}}}}""";

        var json = GirvsSharedConfigMerger.Merge(handWritten,
            [Entry("platform-redis", "redis", ("Endpoints", "localhost:56379"))]);

        using var doc = JsonDocument.Parse(json);
        Assert.Equal("localhost:56379", doc.RootElement.GetProperty("Resources")
            .GetProperty("platform-redis").GetProperty("Settings").GetProperty("Endpoints").GetString());
    }

    [Fact]
    public void 手写文件中未登记的资源原样透传()
    {
        const string handWritten =
            """{"Resources":{"legacy-redis":{"Type":"redis","Settings":{"Endpoints":"10.0.0.8:6379"}}}}""";

        var json = GirvsSharedConfigMerger.Merge(handWritten,
            [Entry("platform-redis", "redis", ("Endpoints", "localhost:56379"))]);

        using var doc = JsonDocument.Parse(json);
        var resources = doc.RootElement.GetProperty("Resources");
        Assert.Equal("10.0.0.8:6379", resources.GetProperty("legacy-redis")
            .GetProperty("Settings").GetProperty("Endpoints").GetString());
        Assert.True(resources.TryGetProperty("platform-redis", out _));
    }
}
