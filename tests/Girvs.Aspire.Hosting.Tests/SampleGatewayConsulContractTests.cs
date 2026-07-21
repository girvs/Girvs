namespace Girvs.Aspire.Hosting.Tests;

public class SampleGatewayConsulContractTests
{
    [Fact]
    public void 样例AppHost_不再启动Consul_网关Run模式使用Aspire()
    {
        var repoRoot = FindRepoRoot();
        var appHostProgram = File.ReadAllText(Path.Combine(repoRoot, "samples", "Sample.AppHost", "Program.cs"));
        var appHostProject = File.ReadAllText(Path.Combine(repoRoot, "samples", "Sample.AppHost", "Sample.AppHost.csproj"));
        var gatewayProgram = File.ReadAllText(Path.Combine(repoRoot, "samples", "Sample.Gateway", "Program.cs"));
        var gatewayStartup = File.ReadAllText(Path.Combine(repoRoot, "samples", "Sample.Gateway", "Startup.cs"));
        var gatewaySwaggerEndpointEnumerator = File.ReadAllText(Path.Combine(repoRoot, "samples", "Sample.Gateway", "SwaggerEndpointEnumerator.cs"));
        var gatewaySwaggerFilterMiddleware = File.ReadAllText(Path.Combine(repoRoot, "samples", "Sample.Gateway", "SwaggerFilterMiddleware.cs"));
        var sampleGatewayProject = File.ReadAllText(Path.Combine(repoRoot, "samples", "Sample.Gateway", "Sample.Gateway.csproj"));
        var gatewayProject = File.ReadAllText(Path.Combine(repoRoot, "Girvs.Aspire.Gateway", "Girvs.Aspire.Gateway.csproj"));
        var openApiProject = File.ReadAllText(Path.Combine(repoRoot, "Girvs.OpenApi", "Girvs.OpenApi.csproj"));
        var serviceAProject = File.ReadAllText(Path.Combine(repoRoot, "samples", "Sample.ServiceA", "Sample.ServiceA.csproj"));
        var serviceBProject = File.ReadAllText(Path.Combine(repoRoot, "samples", "Sample.ServiceB", "Sample.ServiceB.csproj"));
        var serviceASettings = File.ReadAllText(Path.Combine(repoRoot, "samples", "Sample.ServiceA", "appsettings.json"));
        var serviceBSettings = File.ReadAllText(Path.Combine(repoRoot, "samples", "Sample.ServiceB", "appsettings.json"));

        Assert.Contains("builder.ExecutionContext.IsRunMode", appHostProgram);
        Assert.Contains("builder.ExecutionContext.IsPublishMode", appHostProgram);
        Assert.DoesNotContain("AddContainer(\"consul\"", appHostProgram);
        Assert.Contains("Projects.Sample_Gateway", appHostProgram);
        Assert.DoesNotContain("ConsulConfig", appHostProgram);
        Assert.Contains("GatewayDiscovery__DiscoveryType", appHostProgram);
        Assert.Contains("GatewayDiscovery__DiscoveryType\", \"Aspire\"", appHostProgram);
        Assert.Contains("\"Kubernetes\"", appHostProgram);
        Assert.Contains("GirvsHostBuilderManager.CreateGirvsWebApplicationBuilder(args)", gatewayProgram);
        Assert.Contains("app.Run()", gatewayProgram);
        Assert.DoesNotContain("WebApplication.CreateBuilder", gatewayProgram);
        Assert.Contains(": IGirvsStartup", gatewayStartup);
        Assert.Contains("GetSection(\"GatewayDiscovery\")", gatewayStartup);
        Assert.Contains("Get<GatewayDiscoveryConfig>()", gatewayStartup);
        Assert.DoesNotContain("DiscoveryType = GatewayDiscoveryType.Consul", gatewayProgram);
        Assert.DoesNotContain("ConsulAddress", gatewayStartup);
        Assert.Contains("UseSwaggerUI", gatewayStartup);
        Assert.Contains("AddEndpointsApiExplorer", gatewayStartup);
        Assert.Contains("AddSwaggerGen", gatewayStartup);
        Assert.Contains("OpenApiInfo", gatewayStartup);
        Assert.Contains("SwaggerFilterMiddleware", gatewayStartup);
        Assert.Contains("SwaggerEndpointEnumerator", gatewayStartup);
        Assert.DoesNotContain("class SwaggerFilterMiddleware", gatewayStartup);
        Assert.DoesNotContain("class SwaggerEndpointEnumerator", gatewayStartup);
        Assert.Contains("class SwaggerEndpointEnumerator", gatewaySwaggerEndpointEnumerator);
        Assert.Contains("class SwaggerFilterMiddleware", gatewaySwaggerFilterMiddleware);
        Assert.Contains("MapSwagger(\"{documentName}/api-docs\")", gatewayStartup);
        Assert.Contains("GetServices()", gatewaySwaggerEndpointEnumerator);
        Assert.Contains("girvs_openapi/girvs_api.json", gatewaySwaggerEndpointEnumerator);
        Assert.Contains("Yarp.ReverseProxy\" Version=\"2.3.0\"", gatewayProject);
        Assert.Contains("Girvs.OpenApi.csproj", sampleGatewayProject);
        Assert.DoesNotContain("PackageReference Include=\"Swashbuckle.AspNetCore\"", sampleGatewayProject);
        Assert.Contains("PackageReference Include=\"Swashbuckle.AspNetCore\"", openApiProject);
        Assert.DoesNotContain("ServiceA API", gatewayProgram);
        Assert.DoesNotContain("ServiceB API", gatewayProgram);
        Assert.DoesNotContain("Girvs.Consul.csproj", serviceAProject);
        Assert.DoesNotContain("Girvs.Consul.csproj", serviceBProject);
        Assert.Contains("Girvs.OpenApi.csproj", serviceAProject);
        Assert.Contains("Girvs.OpenApi.csproj", serviceBProject);
        Assert.DoesNotContain("\"ConsulConfig\"", serviceASettings);
        Assert.DoesNotContain("\"ConsulConfig\"", serviceBSettings);
        Assert.Contains("Sample.Gateway\\Sample.Gateway.csproj", appHostProject);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "samples"))
                && File.Exists(Path.Combine(directory.FullName, "Girvs.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("未找到仓库根目录。");
    }
}
