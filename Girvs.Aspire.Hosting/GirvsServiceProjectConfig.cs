using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Girvs;

namespace Girvs.Aspire.Hosting;

/// <summary>AppHost 视角的服务治理配置（仅文件型 appsettings.json；环境变量/ConfigMap 注入值读取不到）。</summary>
public sealed record ServiceProjectSettings
{
    public string? ServerName { get; set; }
    public string HealthCheckPath { get; set; } = "/health";
    public string LivenessCheckPath { get; set; } = "/alive";
}

/// <summary>从服务项目 appsettings.json 读取 ModuleConfigurations:ServiceGovernanceConfig 节点。</summary>
internal static class GirvsServiceProjectConfig
{
    public static ServiceProjectSettings ReadFromAppSettings(IProjectMetadata metadata) =>
        ReadFromAppSettings(Path.GetDirectoryName(metadata.ProjectPath));

    public static ServiceProjectSettings ReadFromAppSettings(
        IResourceBuilder<ProjectResource> project
    ) => ReadFromAppSettings(project.Resource.GetProjectMetadata());

    private static ServiceProjectSettings ReadFromAppSettings(string? projectDirectory)
    {
        var settings = new ServiceProjectSettings();
        if (string.IsNullOrWhiteSpace(projectDirectory))
            return settings;
        var appSettingsPath = Path.Combine(projectDirectory, "appsettings.json");
        if (!File.Exists(appSettingsPath))
            return settings;

        try
        {
            // 与 ASP.NET Core 配置读取行为一致:允许注释与尾随逗号
            using var document = JsonDocument.Parse(
                File.ReadAllText(appSettingsPath),
                new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip,
                });
            var root = document.RootElement;
            if (
                root.TryGetProperty("ModuleConfigurations", out var modules)
                && modules.TryGetProperty("ServiceGovernanceConfig", out var governance)
            )
            {
                if (
                    governance.TryGetProperty("ServerName", out var serverName)
                    && serverName.ValueKind == JsonValueKind.String
                )
                    settings.ServerName = serverName.GetString();
                if (governance.TryGetProperty("HealthCheckPath", out var healthPath))
                {
                    // 属性存在但非 JSON string（含 null、数值等）时明确报错，
                    // 不得静默回退默认 /health，避免探针路径与预期不符
                    if (healthPath.ValueKind != JsonValueKind.String)
                        throw new GirvsException(
                            $"ServiceGovernanceConfig 的 HealthCheckPath 必须为字符串，当前类型：{healthPath.ValueKind}"
                        );
                    settings.HealthCheckPath = healthPath.GetString()!;
                }
                if (governance.TryGetProperty("LivenessCheckPath", out var livenessPath))
                {
                    if (livenessPath.ValueKind != JsonValueKind.String)
                        throw new GirvsException(
                            $"ServiceGovernanceConfig 的 LivenessCheckPath 必须为字符串，当前类型：{livenessPath.ValueKind}"
                        );
                    settings.LivenessCheckPath = livenessPath.GetString()!;
                }
            }
        }
        catch (JsonException)
        {
            // appsettings.json 解析失败按缺省值处理，不阻断编排
        }

        ValidateHealthPath(settings.HealthCheckPath, nameof(settings.HealthCheckPath));
        ValidateHealthPath(settings.LivenessCheckPath, nameof(settings.LivenessCheckPath));
        return settings;
    }

    /// <summary>健康检查路径必须为非空且以 / 开头的相对路径，否则无法挂载 HTTP 探针。</summary>
    private static void ValidateHealthPath(string? path, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(path) || !path.StartsWith('/'))
        {
            throw new GirvsException(
                $"ServiceGovernanceConfig 的 {propertyName} 必须为非空且以 / 开头的路径，当前值：'{path}'"
            );
        }
    }
}
