namespace Girvs.Aspire.Hosting.Tests;

public class GirvsProjectExtensionsTests : IDisposable
{
    private readonly string _tempRoot = Directory
        .CreateTempSubdirectory("girvs-project-extensions-tests")
        .FullName;

    public void Dispose() => Directory.Delete(_tempRoot, true);

    private IDistributedApplicationBuilder CreateBuilder(bool publish = false) =>
        DistributedApplication.CreateBuilder(
            new DistributedApplicationOptions
            {
                DisableDashboard = true,
                ProjectDirectory = _tempRoot,
                Args = publish
                    ? ["--operation", "publish", "--publisher", "manifest", "--output-path", "."]
                    : [],
            });

    private IResourceBuilder<ProjectResource> AddServiceProject(
        IDistributedApplicationBuilder builder, string name)
    {
        var csproj = Path.Combine(_tempRoot, $"{name}.csproj");
        File.WriteAllText(csproj, """<Project Sdk="Microsoft.NET.Sdk.Web"></Project>""");
        var project = builder
            .AddProject(name, csproj)
            .WithHttpEndpoint(name: "http");

        // 固定 http endpoint 端口:Run 模式下 WithHttpProbe 隐式注册的健康检查环境回调
        // 会解析该端点地址,未分配端口会使 GetEnvironmentVariableValuesAsync 挂起。
        // 按 Name 精确匹配,避免误伤其他 endpoint。
        var httpEndpoint = project.Resource.Annotations
            .OfType<EndpointAnnotation>()
            .Single(annotation => annotation.Name == "http");
        httpEndpoint.AllocatedEndpoint = new AllocatedEndpoint(
            httpEndpoint, "localhost", 5101);

        return project;
    }

    /// <summary>
    /// 写 launchSettings.json:AddProject 会据此自动声明 http endpoint。
    /// AddGirvsProject 不再自动补充 endpoint,无参/显式入口的项目需依赖该文件。
    /// </summary>
    private static void WriteLaunchSettings(string projectDirectory)
    {
        Directory.CreateDirectory(Path.Combine(projectDirectory, "Properties"));
        File.WriteAllText(
            Path.Combine(projectDirectory, "Properties", "launchSettings.json"),
            """{"profiles":{"http":{"applicationUrl":"http://localhost:5101"}}}""");
    }

    private static void AllocateEndpoint(IResource resource, int port)
    {
        var endpoint = resource.Annotations.OfType<EndpointAnnotation>().First();
        endpoint.AllocatedEndpoint = new AllocatedEndpoint(endpoint, "localhost", port);
    }

    [Fact]
    public void 无参AddGirvsProject_项目元数据类型名_使用统一服务名()
    {
        var builder = CreateBuilder();
        var projectPath = Path.Combine(_tempRoot, "Sample.ServiceA.csproj");
        File.WriteAllText(projectPath, "<Project Sdk=\"Microsoft.NET.Sdk.Web\"></Project>");
        WriteLaunchSettings(_tempRoot);
        Sample_ServiceA.ProjectFilePath = projectPath;

        var project = builder.AddGirvsProject<Sample_ServiceA>();

        Assert.Equal("sample-servicea", project.Resource.Name);
    }

    private sealed class Sample_ServiceA : IProjectMetadata
    {
        public static string ProjectFilePath { get; set; } = "";

        public string ProjectPath => ProjectFilePath;
    }

    [Fact]
    public void 服务对已登记资源自动WaitFor()
    {
        var builder = CreateBuilder();
        var redis = builder.AddRedis("platform-redis").AsGirvsResource();
        var project = AddServiceProject(builder, "service-a");
        builder.AddGirvsProject(project);

        var waits = project.Resource.Annotations.OfType<WaitAnnotation>().ToList();
        Assert.Contains(waits, w => w.Resource == redis.Resource);
    }

    /// <summary>无生命周期的自定义资源(如本地文件型 SQLite),永远不会进入 running 状态。</summary>
    private sealed class FileBackedResource(string name)
        : Resource(name), IResourceWithoutLifetime;

    [Fact]
    public void 无生命周期资源不WaitFor避免服务永久等待()
    {
        var builder = CreateBuilder();
        var sqlite = builder
            .AddResource(new FileBackedResource("sample-sqlite"))
            .AsGirvsResource(type: "sqlite");
        var project = AddServiceProject(builder, "service-a");
        builder.AddGirvsProject(project);

        var waits = project.Resource.Annotations.OfType<WaitAnnotation>().ToList();
        Assert.DoesNotContain(waits, w => w.Resource == sqlite.Resource);
    }

    [Fact]
    public async Task run模式共享文件不存在时自动创建且含登记资源()
    {
        var builder = CreateBuilder();
        var redis = builder.AddRedis("platform-redis").AsGirvsResource();
        AllocateEndpoint(redis.Resource, 56379);
        var project = AddServiceProject(builder, "service-a");
        builder.AddGirvsProject(project);

        var env = await project.Resource.GetEnvironmentVariableValuesAsync(
            DistributedApplicationOperation.Run);

        var path = Assert.Contains("GIRVS_SHARED_CONFIG", env);
        Assert.Equal(Path.Combine(_tempRoot, "girvs.shared.json"), path);
        var content = await File.ReadAllTextAsync(path);
        Assert.Contains("platform-redis", content);
        Assert.Contains("localhost:56379", content);
    }

    [Fact]
    public async Task run模式共享文件已存在时就地更新并保留手写内容()
    {
        var sharedPath = Path.Combine(_tempRoot, "girvs.shared.json");
        File.WriteAllText(sharedPath,
            """{"Logging":{"LogLevel":{"Default":"Warning"}}}""");
        var builder = CreateBuilder();
        var project = AddServiceProject(builder, "service-a");
        builder.AddGirvsProject(project);

        var env = await project.Resource.GetEnvironmentVariableValuesAsync(
            DistributedApplicationOperation.Run);

        Assert.Equal(sharedPath, env["GIRVS_SHARED_CONFIG"]);
        var content = await File.ReadAllTextAsync(sharedPath);
        Assert.Contains("Warning", content);
    }

    [Fact]
    public async Task publish模式注入约定挂载路径且不生成文件()
    {
        var builder = CreateBuilder(publish: true);
        var project = AddServiceProject(builder, "service-a");
        builder.AddGirvsProject(project);

        var env = await project.Resource.GetEnvironmentVariableValuesAsync(
            DistributedApplicationOperation.Publish);

        Assert.Equal("/girvs-config/girvs.shared.json", env["GIRVS_SHARED_CONFIG"]);
        Assert.False(File.Exists(Path.Combine(_tempRoot, "girvs.shared.json")));

#pragma warning disable ASPIREPROBES001 // 实验性探针 API（Aspire 13 默认，K8s 发布器消费）
        var probes = project.Resource.Annotations.OfType<EndpointProbeAnnotation>().ToList();
        Assert.Contains(probes, p => p.Type == ProbeType.Readiness && p.Path == "/health");
        Assert.Contains(probes, p => p.Type == ProbeType.Liveness && p.Path == "/alive");
#pragma warning restore ASPIREPROBES001
    }

    [Fact]
    public void 无参AddGirvsProject_读取appsettings的ServerName作为资源名()
    {
        var builder = CreateBuilder();
        var projectPath = Path.Combine(_tempRoot, "Sample.ServiceA.csproj");
        File.WriteAllText(projectPath, "<Project Sdk=\"Microsoft.NET.Sdk.Web\"></Project>");
        File.WriteAllText(
            Path.Combine(_tempRoot, "appsettings.json"),
            """{"ModuleConfigurations":{"ServiceGovernanceConfig":{"ServerName":"My.Service.A"}}}"""
        );
        WriteLaunchSettings(_tempRoot);
        Sample_ServiceA.ProjectFilePath = projectPath;

        var project = builder.AddGirvsProject<Sample_ServiceA>();

        Assert.Equal("my-service-a", project.Resource.Name);
    }

    [Fact]
    public void 显式传名覆盖appsettings的ServerName()
    {
        var builder = CreateBuilder();
        var projectPath = Path.Combine(_tempRoot, "Sample.ServiceA.csproj");
        File.WriteAllText(projectPath, "<Project Sdk=\"Microsoft.NET.Sdk.Web\"></Project>");
        File.WriteAllText(
            Path.Combine(_tempRoot, "appsettings.json"),
            """{"ModuleConfigurations":{"ServiceGovernanceConfig":{"ServerName":"My.Service.A"}}}"""
        );
        WriteLaunchSettings(_tempRoot);
        Sample_ServiceA.ProjectFilePath = projectPath;

        var project = builder.AddGirvsProject<Sample_ServiceA>("explicit-name");

        Assert.Equal("explicit-name", project.Resource.Name);
    }

#pragma warning disable ASPIREPROBES001 // 实验性探针 API（Aspire 13 默认，K8s 发布器消费）
    [Fact]
    public void AddGirvsProject_声明Readiness与Liveness探针()
    {
        var builder = CreateBuilder();
        var project = AddServiceProject(builder, "service-a");
        builder.AddGirvsProject(project);

        var probes = project.Resource.Annotations.OfType<EndpointProbeAnnotation>().ToList();
        Assert.Contains(probes, p => p.Type == ProbeType.Readiness && p.Path == "/health");
        Assert.Contains(probes, p => p.Type == ProbeType.Liveness && p.Path == "/alive");
    }

    [Fact]
    public void AddGirvsProject_读取appsettings的自定义健康检查路径()
    {
        var builder = CreateBuilder();
        var project = AddServiceProject(builder, "service-a");
        File.WriteAllText(
            Path.Combine(_tempRoot, "appsettings.json"),
            """{"ModuleConfigurations":{"ServiceGovernanceConfig":{"HealthCheckPath":"/healthz","LivenessCheckPath":"/alivez"}}}"""
        );
        builder.AddGirvsProject(project);

        var probes = project.Resource.Annotations.OfType<EndpointProbeAnnotation>().ToList();
        Assert.Contains(probes, p => p.Type == ProbeType.Readiness && p.Path == "/healthz");
        Assert.Contains(probes, p => p.Type == ProbeType.Liveness && p.Path == "/alivez");
    }

    [Fact]
    public void AddGirvsProject_HealthCheckPath不以斜杠开头_抛出GirvsException()
    {
        var builder = CreateBuilder();
        var project = AddServiceProject(builder, "service-a");
        File.WriteAllText(
            Path.Combine(_tempRoot, "appsettings.json"),
            """{"ModuleConfigurations":{"ServiceGovernanceConfig":{"HealthCheckPath":"health"}}}"""
        );

        var exception = Assert.Throws<GirvsException>(
            () => builder.AddGirvsProject(project)
        );

        Assert.Contains("HealthCheckPath", exception.Message);
    }

    [Fact]
    public void AddGirvsProject_LivenessCheckPath不以斜杠开头_抛出GirvsException()
    {
        var builder = CreateBuilder();
        var project = AddServiceProject(builder, "service-a");
        File.WriteAllText(
            Path.Combine(_tempRoot, "appsettings.json"),
            """{"ModuleConfigurations":{"ServiceGovernanceConfig":{"LivenessCheckPath":"alive"}}}"""
        );

        var exception = Assert.Throws<GirvsException>(
            () => builder.AddGirvsProject(project)
        );

        Assert.Contains("LivenessCheckPath", exception.Message);
    }

    [Fact]
    public void AddGirvsProject_appsettings为非法JSON_回退默认Readiness探针()
    {
        var builder = CreateBuilder();
        var project = AddServiceProject(builder, "service-a");
        File.WriteAllText(Path.Combine(_tempRoot, "appsettings.json"), "{invalid json");
        builder.AddGirvsProject(project);

        var probes = project.Resource.Annotations.OfType<EndpointProbeAnnotation>().ToList();
        Assert.Contains(probes, p => p.Type == ProbeType.Readiness && p.Path == "/health");
    }

    [Fact]
    public void AddGirvsProject_appsettings为非法JSON_回退默认Liveness探针()
    {
        var builder = CreateBuilder();
        var project = AddServiceProject(builder, "service-a");
        File.WriteAllText(Path.Combine(_tempRoot, "appsettings.json"), "{invalid json");
        builder.AddGirvsProject(project);

        var probes = project.Resource.Annotations.OfType<EndpointProbeAnnotation>().ToList();
        Assert.Contains(probes, p => p.Type == ProbeType.Liveness && p.Path == "/alive");
    }

    [Fact]
    public void AddGirvsProject_HealthCheckPath为null_抛出GirvsException()
    {
        var builder = CreateBuilder();
        var project = AddServiceProject(builder, "service-a");
        File.WriteAllText(
            Path.Combine(_tempRoot, "appsettings.json"),
            """{"ModuleConfigurations":{"ServiceGovernanceConfig":{"HealthCheckPath":null}}}"""
        );

        var exception = Assert.Throws<GirvsException>(
            () => builder.AddGirvsProject(project)
        );

        Assert.Contains("HealthCheckPath", exception.Message);
    }

    [Fact]
    public void AddGirvsProject_LivenessCheckPath为数值_抛出GirvsException()
    {
        var builder = CreateBuilder();
        var project = AddServiceProject(builder, "service-a");
        File.WriteAllText(
            Path.Combine(_tempRoot, "appsettings.json"),
            """{"ModuleConfigurations":{"ServiceGovernanceConfig":{"LivenessCheckPath":42}}}"""
        );

        var exception = Assert.Throws<GirvsException>(
            () => builder.AddGirvsProject(project)
        );

        Assert.Contains("LivenessCheckPath", exception.Message);
    }
#pragma warning restore ASPIREPROBES001

    [Fact]
    public void AddGirvsProject_无可用HTTPEndpoint_抛出GirvsException()
    {
        var builder = CreateBuilder();
        var projectPath = Path.Combine(_tempRoot, "Sample.ServiceA.csproj");
        File.WriteAllText(projectPath, "<Project Sdk=\"Microsoft.NET.Sdk.Web\"></Project>");
        Sample_ServiceA.ProjectFilePath = projectPath;

        var exception = Assert.Throws<GirvsException>(
            () => builder.AddGirvsProject<Sample_ServiceA>()
        );

        Assert.Contains("HTTP", exception.Message);
    }

    [Fact]
    public void AddGirvsProject_显式名称无可用HTTPEndpoint_抛出GirvsException()
    {
        var builder = CreateBuilder();
        var projectPath = Path.Combine(_tempRoot, "Sample.ServiceA.csproj");
        File.WriteAllText(projectPath, "<Project Sdk=\"Microsoft.NET.Sdk.Web\"></Project>");
        Sample_ServiceA.ProjectFilePath = projectPath;

        var exception = Assert.Throws<GirvsException>(
            () => builder.AddGirvsProject<Sample_ServiceA>("explicit-name")
        );

        Assert.Contains("HTTP", exception.Message);
    }

    [Fact]
    public void AddGirvsProject_已有http端点_不重复添加()
    {
        var builder = CreateBuilder();
        var project = AddServiceProject(builder, "service-a");
        builder.AddGirvsProject(project);

        var httpEndpoints = project.Resource.Annotations
            .OfType<EndpointAnnotation>()
            .Where(endpoint => endpoint.Name == "http")
            .ToList();
        Assert.Single(httpEndpoints);
    }

    [Fact]
    public void AddGirvsProject_已有https端点_不添加http且探针指向https()
    {
        var builder = CreateBuilder();
        var csproj = Path.Combine(_tempRoot, "service-a.csproj");
        File.WriteAllText(csproj, """<Project Sdk="Microsoft.NET.Sdk.Web"></Project>""");
        var project = builder.AddProject("service-a", csproj).WithHttpsEndpoint(name: "https");
        builder.AddGirvsProject(project);

        var httpEndpoints = project.Resource.Annotations
            .OfType<EndpointAnnotation>()
            .Where(endpoint => endpoint.Name == "http")
            .ToList();
        Assert.Empty(httpEndpoints);
#pragma warning disable ASPIREPROBES001 // 实验性探针 API（Aspire 13 默认，K8s 发布器消费）
        var probes = project.Resource.Annotations.OfType<EndpointProbeAnnotation>().ToList();
        Assert.All(probes, probe => Assert.Equal("https", probe.EndpointReference.EndpointName));
#pragma warning restore ASPIREPROBES001
    }

    [Fact]
    public void AddGirvsProject_已有自定义HTTP名端点_不添加http且探针指向自定义名()
    {
        var builder = CreateBuilder();
        var csproj = Path.Combine(_tempRoot, "service-a.csproj");
        File.WriteAllText(csproj, """<Project Sdk="Microsoft.NET.Sdk.Web"></Project>""");
        var project = builder.AddProject("service-a", csproj).WithHttpEndpoint(name: "web");
        builder.AddGirvsProject(project);

        var httpEndpoints = project.Resource.Annotations
            .OfType<EndpointAnnotation>()
            .Where(endpoint => endpoint.Name == "http")
            .ToList();
        Assert.Empty(httpEndpoints);
#pragma warning disable ASPIREPROBES001 // 实验性探针 API（Aspire 13 默认，K8s 发布器消费）
        var probes = project.Resource.Annotations.OfType<EndpointProbeAnnotation>().ToList();
        Assert.All(probes, probe => Assert.Equal("web", probe.EndpointReference.EndpointName));
#pragma warning restore ASPIREPROBES001
    }

    [Fact]
    public void 无参AddGirvsProject_appsettings含注释与尾随逗号_可读ServerName与路径()
    {
        var builder = CreateBuilder();
        var projectPath = Path.Combine(_tempRoot, "Sample.ServiceA.csproj");
        File.WriteAllText(projectPath, "<Project Sdk=\"Microsoft.NET.Sdk.Web\"></Project>");
        File.WriteAllText(
            Path.Combine(_tempRoot, "appsettings.json"),
            """
            {
                // 服务治理配置
                "ModuleConfigurations": {
                    "ServiceGovernanceConfig": {
                        "ServerName": "My.Service.A",
                        "HealthCheckPath": "/healthz",
                        "LivenessCheckPath": "/alivez",
                    },
                },
            }
            """
        );
        WriteLaunchSettings(_tempRoot);
        Sample_ServiceA.ProjectFilePath = projectPath;

        var project = builder.AddGirvsProject<Sample_ServiceA>();

        Assert.Equal("my-service-a", project.Resource.Name);
#pragma warning disable ASPIREPROBES001 // 实验性探针 API（Aspire 13 默认，K8s 发布器消费）
        var probes = project.Resource.Annotations.OfType<EndpointProbeAnnotation>().ToList();
        Assert.Contains(probes, p => p.Type == ProbeType.Readiness && p.Path == "/healthz");
        Assert.Contains(probes, p => p.Type == ProbeType.Liveness && p.Path == "/alivez");
#pragma warning restore ASPIREPROBES001
    }
}
