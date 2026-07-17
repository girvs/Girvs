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
    public void 服务对已登记资源自动WaitFor()
    {
        var builder = CreateBuilder();
        var redis = builder.AddRedis("platform-redis").AsGirvsResource();
        var project = AddServiceProject(builder, "service-a");
        builder.AddGirvsProject(project);

        var waits = project.Resource.Annotations.OfType<WaitAnnotation>().ToList();
        Assert.Contains(waits, w => w.Resource == redis.Resource);
    }

    [Fact]
    public async Task run模式注入运行时共享文件路径且文件内容含登记资源()
    {
        var builder = CreateBuilder();
        var redis = builder.AddRedis("platform-redis").AsGirvsResource();
        AllocateEndpoint(redis.Resource, 56379);
        var project = AddServiceProject(builder, "service-a");
        builder.AddGirvsProject(project);

        var env = await project.Resource.GetEnvironmentVariableValuesAsync(
            DistributedApplicationOperation.Run);

        var path = Assert.Contains("GIRVS_SHARED_CONFIG", env);
        Assert.EndsWith(Path.Combine("obj", "girvs.shared.runtime.json"), path);
        var content = await File.ReadAllTextAsync(path);
        Assert.Contains("platform-redis", content);
        Assert.Contains("localhost:56379", content);
    }

    [Fact]
    public async Task run模式合并AppHost目录手写共享文件()
    {
        File.WriteAllText(Path.Combine(_tempRoot, "girvs.shared.json"),
            """{"Logging":{"LogLevel":{"Default":"Warning"}}}""");
        var builder = CreateBuilder();
        var project = AddServiceProject(builder, "service-a");
        builder.AddGirvsProject(project);

        var env = await project.Resource.GetEnvironmentVariableValuesAsync(
            DistributedApplicationOperation.Run);

        var content = await File.ReadAllTextAsync(env["GIRVS_SHARED_CONFIG"]);
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
        Assert.False(File.Exists(
            Path.Combine(_tempRoot, "obj", "girvs.shared.runtime.json")));
    }
}
