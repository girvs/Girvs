using System.Text.Json;
using System.Text.Json.Nodes;
using Girvs;


namespace Girvs.Aspire.Hosting;

/// <summary>
/// 合并手写共享配置(girvs.shared.json)与 AsGirvsResource 登记的资源条目:
/// 手写内容整体保留,登记资源覆盖 Resources 下同名键。输出就地更新后的共享文件 JSON 文本。
/// </summary>
internal static class GirvsSharedConfigMerger
{
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    public static string Merge(
        string handWrittenJson,
        IEnumerable<KeyValuePair<string, GirvsInfrastructureResource>> entries
    )
    {
        var root = string.IsNullOrWhiteSpace(handWrittenJson)
            ? new JsonObject()
            : ParseRootObject(handWrittenJson);

        if (root["Resources"] is not JsonObject resources)
        {
            resources = new JsonObject();
            root["Resources"] = resources;
        }

        foreach (var (name, resource) in entries)
        {
            var settings = new JsonObject();
            foreach (var (key, value) in resource.Settings)
                settings[key] = value;
            resources[name] = new JsonObject
            {
                ["Type"] = resource.Type,
                ["Settings"] = settings,
            };
        }

        return root.ToJsonString(WriteOptions);
    }

    private static JsonObject ParseRootObject(string json)
    {
        JsonNode? node;
        try
        {
            node = JsonNode.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new GirvsException($"共享配置 girvs.shared.json 不是合法 JSON：{ex.Message}", ex);
        }

        return node as JsonObject
            ?? throw new GirvsException("共享配置 girvs.shared.json 顶层必须是 JSON 对象");
    }
}
