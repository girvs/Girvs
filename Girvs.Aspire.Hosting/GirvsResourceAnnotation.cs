namespace Girvs.Aspire.Hosting;

/// <summary>
/// 标记该 Aspire 资源需登记为 Girvs 共享资源:
/// 容器地址确定后写入运行时共享配置文件的 Resources 节点,键名即资源名。
/// </summary>
public sealed class GirvsResourceAnnotation(
    string typeOverride,
    IReadOnlyDictionary<string, string> extraSettings
) : IResourceAnnotation
{
    /// <summary>覆盖自动推断的资源 Type(如 redis-synchronized-memory);null 表示按资源类型推断。</summary>
    public string TypeOverride { get; } = typeOverride;

    /// <summary>补充或覆盖自动生成的 Settings 键值(如 Ssl、Database)。</summary>
    public IReadOnlyDictionary<string, string> ExtraSettings { get; } =
        extraSettings ?? new Dictionary<string, string>();
}
