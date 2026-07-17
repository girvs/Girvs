namespace Girvs.Aspire.Hosting;

public static class GirvsResourceExtensions
{
    /// <summary>
    /// 把该资源登记为 Girvs 共享资源:资源名即服务端 ConnectionRef 引用的键,
    /// 容器地址在启动期写入运行时共享配置文件(obj/girvs.shared.runtime.json)分发给各 Girvs 服务。
    /// </summary>
    /// <param name="type">覆盖自动推断的资源 Type;不支持的资源类型必须显式指定。</param>
    /// <param name="settings">补充或覆盖自动生成的 Settings 键值。</param>
    public static IResourceBuilder<T> AsGirvsResource<T>(
        this IResourceBuilder<T> builder,
        string type = null,
        IDictionary<string, string> settings = null
    )
        where T : IResource
    {
        return builder.WithAnnotation(
            new GirvsResourceAnnotation(
                type,
                settings is null ? null : new Dictionary<string, string>(settings)
            )
        );
    }
}
