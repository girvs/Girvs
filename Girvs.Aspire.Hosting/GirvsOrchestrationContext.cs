namespace Girvs.Aspire.Hosting;

public class GirvsOrchestrationContext(
    IDistributedApplicationBuilder builder,
    IResourceBuilder<ProjectResource> project,
    IConfiguration serviceSettings,
    GirvsProjectOptions options
)
{
    public IDistributedApplicationBuilder Builder { get; } = builder;
    public IResourceBuilder<ProjectResource> Project { get; } = project;
    public IConfiguration ServiceSettings { get; } = serviceSettings;
    public GirvsProjectOptions Options { get; } = options ?? new GirvsProjectOptions();

    /// <summary>
    /// 按资源名惰性创建资源：已存在（其他服务先创建）则复用，实现多服务共享同一资源实例。
    /// </summary>
    public IResourceBuilder<T> GetOrAddResource<T>(string name, Func<IResourceBuilder<T>> factory)
        where T : class, IResource
    {
        var existing = Builder
            .Resources.OfType<T>()
            .FirstOrDefault(resource =>
                string.Equals(resource.Name, name, StringComparison.OrdinalIgnoreCase)
            );
        return existing is not null ? Builder.CreateResourceBuilder(existing) : factory();
    }
}
