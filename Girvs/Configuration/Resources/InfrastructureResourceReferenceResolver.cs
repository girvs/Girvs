using Girvs.Infrastructure;

namespace Girvs.Configuration.Resources;

/// <summary>
/// 资源引用解析器。
/// 资源引用以数组表示：索引 0 为 Resources 资源键格式串，索引 1 起为 string.Format 参数；
/// 同一组参数既用于组合资源键，也用于格式化资源的 Settings 值。
/// </summary>
public static class InfrastructureResourceReferenceResolver
{
    /// <summary>
    /// 判断资源引用是否已配置：引用非 null、非空数组，且首项不为空白。
    /// </summary>
    public static bool IsConfigured(string[] reference)
    {
        return reference is {Length: > 0} && !string.IsNullOrWhiteSpace(reference[0]);
    }

    /// <summary>
    /// 组合资源键：用引用中的参数格式化 reference[0]。
    /// </summary>
    public static string FormatResourceKey(string[] reference)
    {
        if (!IsConfigured(reference))
            throw new GirvsException("资源引用未配置");

        return FormatValue(reference[0], reference);
    }

    /// <summary>
    /// 用资源引用中的参数（索引 1 起）格式化指定值，例如资源的 Settings:Endpoints。
    /// </summary>
    public static string FormatValue(string format, string[] reference)
    {
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(reference);

        try
        {
            return string.Format(format, reference[1..]);
        }
        catch (FormatException exception)
        {
            // 消息中不嵌入原始格式串：格式串本身含有 { }，容易与异常消息的格式化混淆。
            throw new GirvsException(
                $"资源引用格式串与参数不匹配：{exception.Message}",
                exception
            );
        }
    }

    /// <summary>
    /// 按资源引用组合资源键，从当前 AppSettings.Resources 中取必需资源；可同时校验资源类型。
    /// </summary>
    public static GirvsInfrastructureResource GetRequiredResource(
        string[] reference,
        string expectedType = null
    )
    {
        return GetRequiredResource(GetResources(), reference, expectedType);
    }

    /// <summary>
    /// 按资源引用取资源，读取必需 Setting，并用引用参数格式化该值。
    /// </summary>
    public static string GetRequiredSetting(string[] reference, string settingKey)
    {
        return GetRequiredSetting(GetResources(), reference, settingKey);
    }

    internal static GirvsInfrastructureResource GetRequiredResource(
        IReadOnlyDictionary<string, GirvsInfrastructureResource> resources,
        string[] reference,
        string expectedType = null
    )
    {
        ArgumentNullException.ThrowIfNull(resources);

        var resourceKey = FormatResourceKey(reference);
        return GetRequiredResource(resources, resourceKey, expectedType);
    }

    internal static string GetRequiredSetting(
        IReadOnlyDictionary<string, GirvsInfrastructureResource> resources,
        string[] reference,
        string settingKey
    )
    {
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(settingKey);

        var resourceKey = FormatResourceKey(reference);
        var resource = GetRequiredResource(resources, resourceKey, null);
        var value = resource.Settings?.GetValueOrDefault(settingKey);
        if (string.IsNullOrWhiteSpace(value))
            throw new GirvsException($"Resources:{resourceKey}:Settings:{settingKey} 未配置");

        return FormatValue(value, reference);
    }

    private static IReadOnlyDictionary<string, GirvsInfrastructureResource> GetResources()
    {
        return Singleton<AppSettings>.Instance?.Resources
            ?? throw new GirvsException("AppSettings 尚未初始化，无法解析资源引用");
    }

    private static GirvsInfrastructureResource GetRequiredResource(
        IReadOnlyDictionary<string, GirvsInfrastructureResource> resources,
        string resourceKey,
        string expectedType
    )
    {
        var resource = resources.TryGetValue(resourceKey, out var value)
            ? value
            : throw new GirvsException($"Resources:{resourceKey} 未配置");

        if (
            !string.IsNullOrWhiteSpace(expectedType)
            && !string.Equals(resource.Type, expectedType, StringComparison.OrdinalIgnoreCase)
        )
            throw new GirvsException($"Resources:{resourceKey}:Type 必须为 {expectedType}");

        return resource;
    }
}
