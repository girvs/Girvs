using Girvs;

namespace Girvs.Aspire.Hosting;

public static class GirvsProjectExtensions
{
    /// <summary>
    /// 添加 Girvs 服务项目，并按项目元数据类型名生成与服务发现一致的资源名。
    /// </summary>
    public static IResourceBuilder<ProjectResource> AddGirvsProject<TProject>(
        this IDistributedApplicationBuilder builder
    )
        where TProject : IProjectMetadata, new()
    {
        var metadata = new TProject();
        var settings = GirvsServiceProjectConfig.ReadFromAppSettings(metadata);
        var name = string.IsNullOrWhiteSpace(settings.ServerName)
            ? ServiceNameResolver.FromProjectMetadataName(typeof(TProject).Name)
            : ServiceNameResolver.FromServerName(settings.ServerName);
        return builder.AddGirvsProject(builder.AddProject<TProject>(name));
    }

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
        var settings = GirvsServiceProjectConfig.ReadFromAppSettings(project);
        // 探针必须挂到 http/https 端点;不自动新增 endpoint,缺少时明确报错,
        // 提示通过 launchSettings.json 或显式 WithHttpEndpoint 声明。
        var selectedEndpoint = project.Resource.Annotations
            .OfType<EndpointAnnotation>()
            .FirstOrDefault(endpoint => endpoint.UriScheme is "http" or "https");
        if (selectedEndpoint is null)
        {
            throw new GirvsException(
                $"项目 {project.Resource.Name} 没有可用的 HTTP/HTTPS endpoint，无法声明健康检查探针；请在项目中配置 launchSettings.json 的 http 端点或显式声明 WithHttpEndpoint"
            );
        }

#pragma warning disable ASPIREPROBES001 // 实验性探针 API（Aspire 13 默认，K8s 发布器消费）
        project.WithHttpProbe(ProbeType.Readiness, settings.HealthCheckPath, endpointName: selectedEndpoint.Name);
        project.WithHttpProbe(ProbeType.Liveness, settings.LivenessCheckPath, endpointName: selectedEndpoint.Name);
#pragma warning restore ASPIREPROBES001

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
