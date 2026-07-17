using Microsoft.Extensions.Configuration;

namespace Girvs.Aspire.Hosting.Tests;

public class GirvsSharedConfigurationTests
{
    private class NoResourceModule;

    private static IDistributedApplicationBuilder CreateBuilder() =>
        DistributedApplication.CreateBuilder(
            new DistributedApplicationOptions { DisableDashboard = true }
        );

    private static (IResourceBuilder<ProjectResource> Project, string Directory) AddServiceProject(
        IDistributedApplicationBuilder builder,
        string name
    )
    {
        var directory = Directory.CreateTempSubdirectory($"girvs-shared-{name}").FullName;
        File.WriteAllText(
            Path.Combine(directory, $"{name}.csproj"),
            """<Project Sdk="Microsoft.NET.Sdk.Web"></Project>"""
        );
        var project = builder.AddProject(name, Path.Combine(directory, $"{name}.csproj"));
        return (project, directory);
    }

    [Fact]
    public void 本地Binding字段覆盖共享默认值但继承共享资源名()
    {
        var shared = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string>
                {
                    ["Bindings:cache:Resource"] = "platform-redis",
                    ["Bindings:cache:Options:DefaultDatabase"] = "1",
                }
            )
            .Build();
        var directory = Directory.CreateTempSubdirectory("girvs-shared-config").FullName;
        File.WriteAllText(
            Path.Combine(directory, "appsettings.json"),
            """{ "Bindings": { "cache": { "Options": { "DefaultDatabase": 3 } } } }"""
        );

        var effective = GirvsServiceAppSettings.ReadEffective(shared, directory);

        Assert.Equal("platform-redis", effective["Bindings:cache:Resource"]);
        Assert.Equal("3", effective["Bindings:cache:Options:DefaultDatabase"]);
        Directory.Delete(directory, true);
    }

    [Fact]
    public void 声明共享配置_非敏感项进Settings敏感项进Secrets()
    {
        var builder = CreateBuilder();
        var jwt = builder.AddParameter("jwt-secret", secret: true);

        builder.AddGirvsSharedConfiguration(shared =>
        {
            shared.AddSetting("Logging:LogLevel:Default", "Information");
            shared.AddSecret("Jwt:Secret", jwt);
        });

        var config = GirvsSharedConfigurationExtensions.GetShared(builder);
        Assert.Contains(
            config.Settings,
            s => s.Key == "Logging:LogLevel:Default" && s.Value == "Information"
        );
        Assert.Contains(config.Secrets, s => s.Key == "Jwt:Secret");
        Assert.DoesNotContain(config.Settings, s => s.Key == "Jwt:Secret");
    }

    [Fact]
    public async Task 服务接线后_共享非敏感配置作为环境变量注入()
    {
        var builder = CreateBuilder();
        builder.AddGirvsSharedConfiguration(shared =>
            shared.AddSetting("Logging:LogLevel:Default", "Warning")
        );

        var (project, directory) = AddServiceProject(builder, "svc");
        builder.WireGirvsResources(project, directory, typeof(NoResourceModule));

        var env = await project.Resource.GetEnvironmentVariableValuesAsync(
            DistributedApplicationOperation.Publish
        );
        Assert.Equal("Warning", env["Logging__LogLevel__Default"]);

        Directory.Delete(directory, true);
    }

    [Fact]
    public async Task 两个服务_都注入同一份共享配置()
    {
        var builder = CreateBuilder();
        builder.AddGirvsSharedConfiguration(shared =>
            shared.AddSetting("Logging:LogLevel:Default", "Error")
        );

        var (projectA, dirA) = AddServiceProject(builder, "svc-a");
        var (projectB, dirB) = AddServiceProject(builder, "svc-b");
        builder.WireGirvsResources(projectA, dirA, typeof(NoResourceModule));
        builder.WireGirvsResources(projectB, dirB, typeof(NoResourceModule));

        var envA = await projectA.Resource.GetEnvironmentVariableValuesAsync(
            DistributedApplicationOperation.Publish
        );
        var envB = await projectB.Resource.GetEnvironmentVariableValuesAsync(
            DistributedApplicationOperation.Publish
        );
        Assert.Equal("Error", envA["Logging__LogLevel__Default"]);
        Assert.Equal("Error", envB["Logging__LogLevel__Default"]);

        Directory.Delete(dirA, true);
        Directory.Delete(dirB, true);
    }

    [Fact]
    public async Task 未声明共享配置_服务不注入共享项且不报错()
    {
        var builder = CreateBuilder();
        var (project, directory) = AddServiceProject(builder, "svc");

        var exception = Record.Exception(
            () => builder.WireGirvsResources(project, directory, typeof(NoResourceModule))
        );
        Assert.Null(exception);

        var env = await project.Resource.GetEnvironmentVariableValuesAsync(
            DistributedApplicationOperation.Publish
        );
        Assert.DoesNotContain("Logging__LogLevel__Default", env.Keys);

        Directory.Delete(directory, true);
    }
}
