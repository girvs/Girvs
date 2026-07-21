namespace Girvs.Aspire.Hosting.Tests;

public class SampleGatewayConsulContractTests
{
    [Fact]
    public void Sample_AppHost_编排Consul网关并让ServiceA和ServiceB注册到Consul()
    {
        var repoRoot = FindRepoRoot();
        var appHostProgram = File.ReadAllText(Path.Combine(repoRoot, "samples", "Sample.AppHost", "Program.cs"));
        var appHostProject = File.ReadAllText(Path.Combine(repoRoot, "samples", "Sample.AppHost", "Sample.AppHost.csproj"));
        var gatewayProgram = File.ReadAllText(Path.Combine(repoRoot, "samples", "Sample.Gateway", "Program.cs"));
        var serviceAProject = File.ReadAllText(Path.Combine(repoRoot, "samples", "Sample.ServiceA", "Sample.ServiceA.csproj"));
        var serviceBProject = File.ReadAllText(Path.Combine(repoRoot, "samples", "Sample.ServiceB", "Sample.ServiceB.csproj"));
        var serviceASettings = File.ReadAllText(Path.Combine(repoRoot, "samples", "Sample.ServiceA", "appsettings.json"));
        var serviceBSettings = File.ReadAllText(Path.Combine(repoRoot, "samples", "Sample.ServiceB", "appsettings.json"));

        Assert.Contains("AddContainer(\"consul\", \"hashicorp/consul\"", appHostProgram);
        Assert.Contains("Projects.Sample_Gateway", appHostProgram);
        Assert.Contains("ModuleConfigurations__ConsulConfig__ConsulAddress", appHostProgram);
        Assert.Contains("ModuleConfigurations__ConsulConfig__HealthAddress", appHostProgram);
        Assert.Contains("GatewayDiscoveryType.Consul", gatewayProgram);
        Assert.Contains("ConsulAddress", gatewayProgram);
        Assert.Contains("Girvs.Consul.csproj", serviceAProject);
        Assert.Contains("Girvs.Consul.csproj", serviceBProject);
        Assert.Contains("\"ConsulConfig\"", serviceASettings);
        Assert.Contains("\"ConsulConfig\"", serviceBSettings);
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
