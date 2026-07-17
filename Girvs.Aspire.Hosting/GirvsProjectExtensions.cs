namespace Girvs.Aspire.Hosting;

public static class GirvsProjectExtensions
{
    /// <summary>
    /// 添加 Girvs 服务项目:注入 GIRVS_SHARED_CONFIG 共享配置文件路径,
    /// 并对所有已通过 AsGirvsResource 登记的资源 WaitFor。
    /// 约定:先编排基础资源(AsGirvsResource),再 AddGirvsProject。
    /// 服务用不用某资源、用哪个,由服务自己配置中的 ConnectionRef 决定。
    /// </summary>
    public static IResourceBuilder<ProjectResource> AddGirvsProject<TProject>(
        this IDistributedApplicationBuilder builder,
        string name
    )
        where TProject : IProjectMetadata, new()
    {
        return builder.AddGirvsProject(builder.AddProject<TProject>(name));
    }

    internal static IResourceBuilder<ProjectResource> AddGirvsProject(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> project
    )
    {
        if (builder.ExecutionContext.IsPublishMode)
        {
            // 生产:共享文件由 K8s ConfigMap 挂载到约定路径,内容与地址由运维维护
            project.WithEnvironment(
                GirvsSharedConfigFile.EnvName,
                GirvsSharedConfigFile.PublishMountPath
            );
            return project;
        }

        foreach (var resource in builder.Resources)
        {
            // 无生命周期资源(IResourceWithoutLifetime,如本地文件型 SQLite)永远不会进入
            // running 状态,WaitFor 会让服务永久等待,跳过
            if (
                resource.Annotations.OfType<GirvsResourceAnnotation>().Any()
                && resource is not IResourceWithoutLifetime
            )
                project.WaitFor(builder.CreateResourceBuilder(resource));
        }

        // 环境回调在服务启动前求值,此时 WaitFor 的容器 endpoint 已分配,可安全写文件
        project.WithEnvironment(async context =>
        {
            var path = await GirvsSharedConfigFile.EnsureWrittenAsync(
                builder,
                context.CancellationToken
            );
            context.EnvironmentVariables[GirvsSharedConfigFile.EnvName] = path;
        });

        return project;
    }
}
