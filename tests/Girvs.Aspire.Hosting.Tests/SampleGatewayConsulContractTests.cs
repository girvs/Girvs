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
        var gatewayProject = File.ReadAllText(Path.Combine(repoRoot, "Girvs.Aspire.Gateway", "Girvs.Aspire.Gateway.csproj"));
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
        Assert.Contains("GetSection(\"GatewayDiscovery\")", gatewayProgram);
        Assert.Contains("Get<GatewayDiscoveryConfig>()", gatewayProgram);
        Assert.Contains("ConfigureApplicationServices", gatewayProgram);
        Assert.DoesNotContain("DiscoveryType = GatewayDiscoveryType.Consul", gatewayProgram);
        Assert.DoesNotContain("ConsulAddress", gatewayProgram);
        Assert.Contains("UseSwaggerUI", gatewayProgram);
        Assert.Contains("AddEndpointsApiExplorer", gatewayProgram);
        Assert.Contains("AddSwaggerGen", gatewayProgram);
        Assert.Contains("OpenApiInfo", gatewayProgram);
        Assert.Contains("SwaggerFilterMiddleware", gatewayProgram);
        Assert.Contains("SwaggerEndpointEnumerator", gatewayProgram);
        Assert.Contains("MapSwagger(\"{documentName}/api-docs\")", gatewayProgram);
        Assert.Contains("GetServices()", gatewayProgram);
        Assert.Contains("girvs_openapi/girvs_api.json", gatewayProgram);
        Assert.Contains("Yarp.ReverseProxy\" Version=\"2.3.0\"", gatewayProject);
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
