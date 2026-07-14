namespace Girvs.Aspire.Hosting;

public static class GirvsProjectExtensions
{
    /// <summary>
    /// 添加 Girvs 服务项目：递归遍历根模块的 [DependsOn] 声明，
    /// 自动创建/复用对应的 Aspire 资源并 WithReference + WaitFor。
    /// </summary>
    public static IResourceBuilder<ProjectResource> AddGirvsProject<TProject>(
        this IDistributedApplicationBuilder builder,
        string name,
        Type rootModuleType,
        Action<GirvsProjectOptions> configure = null
    )
        where TProject : IProjectMetadata, new()
    {
        var options = new GirvsProjectOptions();
        configure?.Invoke(options);

        var project = builder.AddProject<TProject>(name);
        var projectDirectory = Path.GetDirectoryName(new TProject().ProjectPath);
        return builder.WireGirvsResources(project, projectDirectory, rootModuleType, options);
    }

    internal static IResourceBuilder<ProjectResource> WireGirvsResources(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> project,
        string projectDirectory,
        Type rootModuleType,
        GirvsProjectOptions options = null
    )
    {
        var serviceSettings = GirvsServiceAppSettings.Read(projectDirectory);
        var context = new GirvsOrchestrationContext(builder, project, serviceSettings, options);

        foreach (var moduleType in DependsOnGraph.Collect(rootModuleType))
        {
            if (GirvsResourceRegistry.TryGet(moduleType, out var contributor))
                contributor.Contribute(context);
        }

        return project;
    }
}
