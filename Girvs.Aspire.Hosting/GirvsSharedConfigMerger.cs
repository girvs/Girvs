using System.Text.Json;
using System.Text.Json.Nodes;
using GirvsResource = Girvs.Configuration.Resources.Resource;

namespace Girvs.Aspire.Hosting;

/// <summary>
/// 合并手写共享配置(girvs.shared.json)与 AsGirvsResource 登记的资源条目:
/// 手写内容整体保留,登记资源覆盖 Resources 下同名键。输出运行时共享文件的 JSON 文本。
/// </summary>
internal static class GirvsSharedConfigMerger
{
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    public static string Merge(
        string handWrittenJson,
        IEnumerable<KeyValuePair<string, GirvsResource>> entries
    )
    {
        var root = string.IsNullOrWhiteSpace(handWrittenJson)
            ? new JsonObject()
            : JsonNode.Parse(handWrittenJson)!.AsObject();

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
}
