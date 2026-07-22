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
        return builder.AddProject(name, csproj);
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
    }
}
